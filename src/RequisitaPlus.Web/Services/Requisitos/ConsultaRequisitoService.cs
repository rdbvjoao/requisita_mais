using Microsoft.EntityFrameworkCore;
using RequisitaPlus.Web.Models;
using RequisitaPlus.Web.ViewModels.Requisitos;

namespace RequisitaPlus.Web.Services.Requisitos;

public sealed class ConsultaRequisitoService(AcessoProprietarioService acesso)
{
    public async Task<ListagemRequisitosViewModel> PainelAsync(FiltrosRequisitosViewModel filtros, CancellationToken ct = default)
    {
        // Todas as consultas partem do acesso por proprietário. Os indicadores não dependem dos filtros.
        var requisitos = acesso.Requisitos().AsNoTracking();
        var totais = await requisitos.GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Quantidade = g.Count() }).ToListAsync(ct);
        var analisados = await acesso.Analises().AsNoTracking()
            .Select(x => x.IdRequisito).Distinct().CountAsync(ct);
        var pendentes = await acesso.Sugestoes().AsNoTracking().CountAsync(x => x.Decisao == "Pendente", ct);
        var indicadores = new IndicadoresRequisitosViewModel(
            totais.Where(x => x.Status != "Arquivado").Sum(x => x.Quantidade), analisados, pendentes,
            totais.Where(x => x.Status == "Arquivado").Sum(x => x.Quantidade));

        // Sem seleção explícita, mantém a listagem de ativos. Arquivados continuam somente leitura.
        var consulta = requisitos.Where(x => filtros.Status == "Arquivado"
            ? x.Status == "Arquivado" : x.Status != "Arquivado");
        var pesquisa = filtros.Pesquisa?.Trim();
        if (!string.IsNullOrEmpty(pesquisa))
            consulta = consulta.Where(x => x.Titulo.Contains(pesquisa));
        if (!string.IsNullOrEmpty(filtros.Prioridade))
            consulta = consulta.Where(x => x.Prioridade == filtros.Prioridade);
        if (!string.IsNullOrEmpty(filtros.Status))
            consulta = consulta.Where(x => x.Status == filtros.Status);

        // Somente os resultados da tabela são materializados; nenhum histórico é carregado.
        var registros = await consulta.OrderByDescending(x => x.DataCadastroUtc).ThenByDescending(x => x.IdRequisito)
            .ToListAsync(ct);
        return new(filtros, indicadores, registros.Select(Projetar).ToArray());
    }

    public async Task<IReadOnlyList<DetalhesRequisitoViewModel>> ListarAsync(bool arquivados = false, CancellationToken ct = default)
    {
        var registros = await acesso.Requisitos().AsNoTracking()
            .Where(x => arquivados ? x.Status == "Arquivado" : x.Status != "Arquivado")
            .OrderByDescending(x => x.DataCadastroUtc).ThenByDescending(x => x.IdRequisito).ToListAsync(ct);
        return registros.Select(Projetar).ToArray();
    }

    public async Task<DetalhesRequisitoViewModel?> DetalhesAsync(int id, CancellationToken ct = default)
    {
        var requisito = await acesso.Requisitos().AsNoTracking().SingleOrDefaultAsync(x => x.IdRequisito == id, ct);
        return requisito is null ? null : Projetar(requisito);
    }

    private static DetalhesRequisitoViewModel Projetar(Requisito x) => new(x.IdRequisito, x.Titulo,
        x.Descricao, x.Prioridade, x.Status, x.VersaoTextual, x.DataCadastroUtc,
        x.DataAtualizacaoUtc, Convert.ToBase64String(x.RowVersion));
}

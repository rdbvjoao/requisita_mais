using Microsoft.AspNetCore.Http;
using RequisitaPlus.Tests.Support;
using RequisitaPlus.Web.Models;
using RequisitaPlus.Web.Services.Requisitos;
using RequisitaPlus.Web.ViewModels.Requisitos;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

// Usa exclusivamente o contexto simulado: nenhuma conexão SQL ou chamada ao Ollama.
public sealed class PainelRequisitosTests
{
    private static AnaliseSimulada Dados()
    {
        var dados = new AnaliseSimulada();
        dados.Requisitos.Clear();
        for (var id = 1; id <= 5; id++)
            dados.Requisitos.Add(new Requisito
            {
                IdRequisito = id, IdUsuario = id == 5 ? 2 : 1,
                Titulo = id == 4 ? "Pagamento com cartão" : "Cadastro " + id,
                Descricao = "Descrição preservada", Prioridade = id == 4 ? "Média" : "Alta",
                Status = id switch { 2 => "Analisado", 3 => "Arquivado", 4 => "Revisado", _ => "Rascunho" },
                DataCadastroUtc = TestData.Instant.AddDays(id == 4 ? 2 : 0), RowVersion = BitConverter.GetBytes((long)id)
            });
        foreach (var (id, requisito) in new[] { (1, 1), (2, 1), (3, 3), (4, 5) })
        {
            var operacao = Guid.NewGuid();
            dados.Operacoes.Add(new OperacaoAnalise { IdOperacao = operacao, IdRequisito = requisito, IdUsuario = requisito == 5 ? 2 : 1 });
            dados.Analises.Add(new Analise { IdAnalise = id, IdRequisito = requisito, IdOperacao = operacao });
        }
        // Uma falha sem análise persistida não contribui para os indicadores de IA.
        dados.Operacoes.Add(new OperacaoAnalise { IdOperacao = Guid.NewGuid(), IdRequisito = 4, IdUsuario = 1, Estado = "FalhaEncerrada" });
        foreach (var (id, analise, decisao) in new[] { (1, 1, "Pendente"), (2, 2, "Pendente"), (3, 3, "Pendente"), (4, 1, "Descartada"), (5, 4, "Pendente") })
            dados.Sugestoes.Add(new Sugestao { IdSugestao = id, IdAnalise = analise, OrdemNoResultado = id, Decisao = decisao });
        return dados;
    }

    private static async Task<ListagemRequisitosViewModel> Consultar(AnaliseSimulada dados, FiltrosRequisitosViewModel filtros, int? usuario = 1)
    {
        using var db = dados.Contexto();
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        if (usuario.HasValue) http.HttpContext.User = TestData.Principal(usuario.Value);
        var resultado = await new ConsultaRequisitoService(new(db, http)).PainelAsync(filtros, TestContext.Current.CancellationToken);
        Assert.All(db.ChangeTracker.Entries(), entry => Assert.Equal(Microsoft.EntityFrameworkCore.EntityState.Unchanged, entry.State));
        return resultado;
    }

    [Fact]
    public async Task IndicatorsCountDistinctPersistedAnalysesAndIndividualPendingSuggestionsIncludingArchives()
    {
        var dados = Dados();
        var painel = await Consultar(dados, new());
        Assert.Equal(new(3, 2, 3, 1), painel.Indicadores);
        Assert.Equal(new[] { 4, 2, 1 }, painel.Requisitos.Select(x => x.IdRequisito));
        Assert.Equal("Analisado", painel.Requisitos.Single(x => x.IdRequisito == 2).Status);
        Assert.Equal("Rascunho", painel.Requisitos.Single(x => x.IdRequisito == 1).Status);
        var filtrado = await Consultar(dados, new() { Pesquisa = "inexistente" });
        Assert.Empty(filtrado.Requisitos);
        Assert.Equal(painel.Indicadores, filtrado.Indicadores);
    }

    [Theory]
    [InlineData("Cadastro", "Alta", "Analisado", 2)]
    [InlineData("  Pagamento  ", "Média", "Revisado", 4)]
    [InlineData(null, null, "Arquivado", 3)]
    [InlineData(null, "Alta", "Rascunho", 1)]
    public async Task FiltersCombineAndPreserveInput(string? pesquisa, string? prioridade, string? status, int esperado)
    {
        var filtros = new FiltrosRequisitosViewModel { Pesquisa = pesquisa, Prioridade = prioridade, Status = status };
        var resultado = await Consultar(Dados(), filtros);
        Assert.Same(filtros, resultado.Filtros);
        Assert.Equal(esperado, Assert.Single(resultado.Requisitos).IdRequisito);
    }

    [Theory]
    [InlineData(2, 1, 1, 1, 0)]
    [InlineData(99, 0, 0, 0, 0)]
    [InlineData(null, 0, 0, 0, 0)]
    public async Task EveryQueryRespectsOwnerIncludingMissingIdentity(int? usuario, int ativos, int analisados, int pendentes, int arquivados)
    {
        var resultado = await Consultar(Dados(), new(), usuario);
        Assert.Equal(new(ativos, analisados, pendentes, arquivados), resultado.Indicadores);
        Assert.Equal(ativos, resultado.Requisitos.Count);
        if (usuario == 2) Assert.Equal(5, Assert.Single(resultado.Requisitos).IdRequisito);
    }
}

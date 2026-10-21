using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequisitaPlus.Web.Services.IA;
using RequisitaPlus.Web.Services.Requisitos;
using RequisitaPlus.Web.ViewModels.Analises;

namespace RequisitaPlus.Web.Controllers;

[Authorize]
public sealed class AnalisesController(AcessoProprietarioService acesso, ConsultaRequisitoService consulta,
    AnaliseService analises) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Confirmar([FromRoute] int id, CancellationToken ct)
    {
        var requisito = await consulta.DetalhesAsync(id, ct);
        if (requisito is null) return NotFound("Requisito não encontrado");
        if (requisito.Status == "Arquivado") return Conflict("Requisitos arquivados são somente leitura.");
        return View(new ConfirmarAnaliseViewModel { IdOperacao = Guid.NewGuid(), Versao = requisito.VersaoTextual,
            RowVersion = requisito.RowVersion, Requisito = requisito });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Iniciar([FromRoute] int id,
        [Bind("IdOperacao,Versao,RowVersion,Confirmacao")] ConfirmarAnaliseViewModel entrada, CancellationToken ct)
    {
        var requisito = await consulta.DetalhesAsync(id, ct);
        if (requisito is null) return NotFound("Requisito não encontrado");
        if (requisito.Status == "Arquivado") return Conflict("Requisitos arquivados são somente leitura.");
        entrada.Requisito = requisito;
        if (!ModelState.IsValid) return View("Confirmar", entrada);
        var usuario = acesso.UsuarioAtual();
        if (usuario is null) return Challenge();
        var resultado = await analises.IniciarAsync(new(id, usuario.Value, entrada.IdOperacao, entrada.Versao, entrada.RowVersion));
        if (resultado.NaoEncontrado) return NotFound("Requisito não encontrado");
        if (resultado.Erro is not null)
        {
            Response.StatusCode = 409;
            ModelState.AddModelError("", resultado.Erro);
            return View("Confirmar", entrada);
        }
        return RedirectToAction(nameof(Operacao), new { id = resultado.IdOperacao });
    }

    [HttpGet]
    public async Task<IActionResult> Operacao([FromRoute] Guid id, CancellationToken ct)
    {
        var operacao = await acesso.Operacoes().AsNoTracking().SingleOrDefaultAsync(x => x.IdOperacao == id, ct);
        if (operacao is null) return NotFound("Operação não encontrada");
        var analise = await acesso.Analises().AsNoTracking().SingleOrDefaultAsync(x => x.IdOperacao == id, ct);
        if (analise is not null) return RedirectToAction(nameof(Detalhes), new { id = analise.IdAnalise });
        return View(operacao);
    }

    [HttpGet]
    public async Task<IActionResult> Historico([FromRoute] int id, CancellationToken ct)
    {
        var requisito = await consulta.DetalhesAsync(id, ct);
        if (requisito is null) return NotFound("Requisito não encontrado");
        var registros = await acesso.Analises().AsNoTracking().Where(x => x.IdRequisito == id)
            .OrderByDescending(x => x.OrdemPersistencia).ToListAsync(ct);
        return View(new HistoricoAnaliseViewModel(requisito, registros));
    }

    [HttpGet]
    public async Task<IActionResult> Detalhes([FromRoute] int id, CancellationToken ct)
    {
        var analise = await acesso.Analises().AsNoTracking().SingleOrDefaultAsync(x => x.IdAnalise == id, ct);
        if (analise is null) return NotFound("Análise não encontrada");
        var requisito = await consulta.DetalhesAsync(analise.IdRequisito, ct);
        var atual = requisito!.VersaoTextual == analise.VersaoTextualAnalisada &&
            !await acesso.Analises().AnyAsync(x => x.IdRequisito == analise.IdRequisito &&
                x.VersaoTextualAnalisada == requisito.VersaoTextual && x.OrdemPersistencia > analise.OrdemPersistencia, ct);
        var sugestoes = await acesso.Sugestoes().AsNoTracking().Where(x => x.IdAnalise == id)
            .OrderBy(x => x.OrdemNoResultado).ToListAsync(ct);
        return View(new ResultadoAnaliseViewModel(analise, sugestoes, atual));
    }
}

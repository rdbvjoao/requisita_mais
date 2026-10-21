using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequisitaPlus.Web.Services.Requisitos;
using RequisitaPlus.Web.ViewModels.Requisitos;

namespace RequisitaPlus.Web.Controllers;

[Authorize]
public sealed class RequisitosController(ConsultaRequisitoService consulta, CadastroRequisitoService cadastro,
    EdicaoRequisitoService edicao, ArquivamentoRequisitoService arquivamento) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] FiltrosRequisitosViewModel filtros, CancellationToken ct) =>
        View(await consulta.PainelAsync(filtros, ct));

    [HttpGet]
    public async Task<IActionResult> Arquivados(CancellationToken ct) => View(await consulta.ListarAsync(true, ct));

    [HttpGet]
    public async Task<IActionResult> Detalhes([FromRoute] int id, CancellationToken ct)
    {
        var requisito = await consulta.DetalhesAsync(id, ct);
        return requisito is null ? NaoEncontrado() : View(requisito);
    }

    [HttpGet]
    public IActionResult Criar() => View(new CriarRequisitoViewModel());

    [HttpPost]
    public async Task<IActionResult> Criar(CriarRequisitoViewModel entrada, CancellationToken ct)
    {
        RejeitarCamposControlados();
        if (!ModelState.IsValid) return View(entrada);
        try
        {
            var id = await cadastro.CriarAsync(entrada, ct);
            TempData["Sucesso"] = "Requisito cadastrado com sucesso.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }
        catch (DbUpdateException) { ErroGravacao(); return View(entrada); }
    }

    [HttpGet]
    public async Task<IActionResult> Editar([FromRoute] int id, CancellationToken ct)
    {
        var requisito = await consulta.DetalhesAsync(id, ct);
        if (requisito is null) return NaoEncontrado();
        if (requisito.Status == "Arquivado") return Arquivado();
        return View(new EditarRequisitoViewModel
        {
            Titulo = requisito.Titulo, Descricao = requisito.Descricao, Prioridade = requisito.Prioridade,
            VersaoEsperada = requisito.VersaoTextual, RowVersion = requisito.RowVersion
        });
    }

    [HttpPost]
    public async Task<IActionResult> Editar([FromRoute] int id, EditarRequisitoViewModel entrada, CancellationToken ct)
    {
        // Authorize even invalid direct submissions, using the same unavailable response.
        var requisito = await consulta.DetalhesAsync(id, ct);
        if (requisito is null) return NaoEncontrado();
        if (requisito.Status == "Arquivado") return Arquivado();
        RejeitarCamposControlados();
        if (!ModelState.IsValid) return View(entrada);
        try
        {
            var resultado = await edicao.EditarAsync(id, entrada, ct);
            if (resultado == ResultadoMutacaoRequisito.NaoEncontrado) return NaoEncontrado();
            if (resultado == ResultadoMutacaoRequisito.Arquivado) return Arquivado();
            if (resultado == ResultadoMutacaoRequisito.Conflito)
            {
                Response.StatusCode = StatusCodes.Status409Conflict;
                ModelState.AddModelError(string.Empty, "O requisito foi alterado em outra operação. Recarregue a página antes de tentar novamente.");
                return View(entrada);
            }
            TempData["Sucesso"] = "Requisito atualizado com sucesso.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }
        catch (DbUpdateException) { ErroGravacao(); return View(entrada); }
    }

    [HttpPost]
    public async Task<IActionResult> Arquivar([FromRoute] int id, ConcorrenciaRequisitoViewModel entrada, CancellationToken ct)
    {
        var requisito = await consulta.DetalhesAsync(id, ct);
        if (requisito is null) return NaoEncontrado();
        if (requisito.Status == "Arquivado") return Arquivado();
        RejeitarCamposControlados();
        if (!ModelState.IsValid) return View(nameof(Detalhes), requisito);
        try
        {
            var resultado = await arquivamento.ArquivarAsync(id, entrada, ct);
            if (resultado == ResultadoMutacaoRequisito.NaoEncontrado) return NaoEncontrado();
            if (resultado == ResultadoMutacaoRequisito.Arquivado) return Arquivado();
            if (resultado == ResultadoMutacaoRequisito.Conflito)
            {
                Response.StatusCode = StatusCodes.Status409Conflict;
                ModelState.AddModelError(string.Empty, "O requisito foi alterado em outra operação. Recarregue os detalhes e confirme novamente.");
                return View(nameof(Detalhes), requisito);
            }
            TempData["Sucesso"] = "Requisito arquivado com sucesso. O histórico foi preservado.";
            return RedirectToAction(nameof(Arquivados));
        }
        catch (DbUpdateException) { ErroGravacao(); return View(nameof(Detalhes), requisito); }
    }

    private void RejeitarCamposControlados()
    {
        if (Request.HasFormContentType && Request.Form.Keys.Any(x =>
                new[] { "Status", "IdUsuario", "OwnerId", "IdRequisito", "VersaoTextual", "DataCadastroUtc", "DataAtualizacaoUtc", "UltimaOrdemAnalise" }
                    .Contains(x, StringComparer.OrdinalIgnoreCase)))
            ModelState.AddModelError(string.Empty, "O formulário contém campos controlados pelo sistema. Recarregue a página.");
    }

    private void ErroGravacao() => ModelState.AddModelError(string.Empty,
        "Não foi possível salvar o requisito. Verifique os dados e tente novamente.");
    private NotFoundObjectResult NaoEncontrado() => NotFound("Requisito não encontrado");
    private ObjectResult Arquivado() => StatusCode(StatusCodes.Status409Conflict, "Requisitos arquivados são somente leitura.");
}

using System.ComponentModel.DataAnnotations;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.Models;
using RequisitaPlus.Web.ViewModels.Requisitos;

namespace RequisitaPlus.Web.Services.Requisitos;

public sealed class CadastroRequisitoService(ApplicationDbContext db, AcessoProprietarioService acesso, RelogioLocal relogio)
{
    public async Task<int> CriarAsync(CriarRequisitoViewModel entrada, CancellationToken ct = default)
    {
        Validator.ValidateObject(entrada, new ValidationContext(entrada), true);
        var usuario = acesso.UsuarioAtual() ?? throw new UnauthorizedAccessException();
        var requisito = new Requisito
        {
            IdUsuario = usuario, Titulo = entrada.Titulo, Descricao = entrada.Descricao,
            Prioridade = entrada.Prioridade, Status = "Rascunho", VersaoTextual = 1,
            UltimaOrdemAnalise = 0, DataCadastroUtc = relogio.AgoraUtc
        };
        db.Requisitos.Add(requisito);
        await db.SaveChangesAsync(ct);
        return requisito.IdRequisito;
    }
}

using System.ComponentModel.DataAnnotations;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.ViewModels.Requisitos;

namespace RequisitaPlus.Web.Services.Requisitos;

public sealed class ArquivamentoRequisitoService(ApplicationDbContext db, AcessoProprietarioService acesso, RelogioLocal relogio)
{
    public async Task<ResultadoMutacaoRequisito> ArquivarAsync(int id, ConcorrenciaRequisitoViewModel entrada, CancellationToken ct = default)
    {
        Validator.ValidateObject(entrada, new ValidationContext(entrada), true);
        var requisito = await acesso.RequisitoAsync(id, ct);
        if (requisito is null) return ResultadoMutacaoRequisito.NaoEncontrado;
        if (requisito.Status == "Arquivado") return ResultadoMutacaoRequisito.Arquivado;
        if (!ConcorrenciaRequisito.Corresponde(requisito, entrada.RowVersion)) return ResultadoMutacaoRequisito.Conflito;
        db.Entry(requisito).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(entrada.RowVersion);
        requisito.Status = "Arquivado";
        requisito.DataAtualizacaoUtc = relogio.AgoraUtc;
        return await ConcorrenciaRequisito.SalvarAsync(db, ct);
    }
}

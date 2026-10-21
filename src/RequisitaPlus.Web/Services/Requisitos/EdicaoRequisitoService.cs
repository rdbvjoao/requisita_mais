using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.Models;
using RequisitaPlus.Web.ViewModels.Requisitos;

namespace RequisitaPlus.Web.Services.Requisitos;

public enum ResultadoMutacaoRequisito { Concluido, NaoEncontrado, Arquivado, Conflito }

public sealed class EdicaoRequisitoService(ApplicationDbContext db, AcessoProprietarioService acesso, RelogioLocal relogio)
{
    public async Task<ResultadoMutacaoRequisito> EditarAsync(int id, EditarRequisitoViewModel entrada, CancellationToken ct = default)
    {
        Validator.ValidateObject(entrada, new ValidationContext(entrada), true);
        var requisito = await acesso.RequisitoAsync(id, ct);
        if (requisito is null) return ResultadoMutacaoRequisito.NaoEncontrado;
        if (requisito.Status == "Arquivado") return ResultadoMutacaoRequisito.Arquivado;
        if (requisito.VersaoTextual != entrada.VersaoEsperada || !ConcorrenciaRequisito.Corresponde(requisito, entrada.RowVersion))
            return ResultadoMutacaoRequisito.Conflito;

        var textoAlterado = !string.Equals(requisito.Titulo, entrada.Titulo, StringComparison.Ordinal)
            || !string.Equals(requisito.Descricao, entrada.Descricao, StringComparison.Ordinal);
        var prioridadeAlterada = !string.Equals(requisito.Prioridade, entrada.Prioridade, StringComparison.Ordinal);
        if (!textoAlterado && !prioridadeAlterada) return ResultadoMutacaoRequisito.Concluido;

        db.Entry(requisito).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(entrada.RowVersion);
        requisito.Titulo = entrada.Titulo;
        requisito.Descricao = entrada.Descricao;
        requisito.Prioridade = entrada.Prioridade;
        if (textoAlterado)
        {
            requisito.VersaoTextual = checked(requisito.VersaoTextual + 1);
            requisito.Status = "Rascunho";
        }
        requisito.DataAtualizacaoUtc = relogio.AgoraUtc;
        return await ConcorrenciaRequisito.SalvarAsync(db, ct);
    }
}

internal static class ConcorrenciaRequisito
{
    internal static bool Corresponde(Requisito requisito, string token) =>
        requisito.RowVersion.AsSpan().SequenceEqual(Convert.FromBase64String(token));

    internal static async Task<ResultadoMutacaoRequisito> SalvarAsync(ApplicationDbContext db, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return ResultadoMutacaoRequisito.Concluido;
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoMutacaoRequisito.Conflito;
        }
    }
}

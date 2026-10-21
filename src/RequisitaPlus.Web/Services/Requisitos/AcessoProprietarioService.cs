using System.Globalization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.Models;

namespace RequisitaPlus.Web.Services.Requisitos;

public sealed class AcessoProprietarioService(ApplicationDbContext db, IHttpContextAccessor http)
{
    public int? UsuarioAtual()
    {
        var identity = http.HttpContext?.User.Identities.FirstOrDefault(x => x.IsAuthenticated);
        var claims = identity?.FindAll(ClaimTypes.NameIdentifier).ToArray();
        return claims is { Length: 1 } && int.TryParse(claims[0].Value, NumberStyles.None,
            CultureInfo.InvariantCulture, out var id) && id > 0 ? id : null;
    }

    public IQueryable<Requisito> Requisitos()
    {
        var usuario = UsuarioAtual() ?? 0;
        return db.Requisitos.Where(x => usuario > 0 && x.IdUsuario == usuario);
    }
    public IQueryable<Analise> Analises()
    {
        var usuario = UsuarioAtual() ?? 0;
        return db.Analises.Where(x => usuario > 0 && x.Requisito.IdUsuario == usuario);
    }
    public IQueryable<Sugestao> Sugestoes()
    {
        var usuario = UsuarioAtual() ?? 0;
        return db.Sugestoes.Where(x => usuario > 0 && x.Analise.Requisito.IdUsuario == usuario);
    }
    public IQueryable<OperacaoAnalise> Operacoes()
    {
        var usuario = UsuarioAtual() ?? 0;
        return db.OperacoesAnalise.Where(x => usuario > 0 && x.IdUsuario == usuario
            && x.Requisito.IdUsuario == usuario);
    }
    // Alheio e inexistente retornam null pela mesma consulta filtrada.
    public Task<Requisito?> RequisitoAsync(int id, CancellationToken ct = default) =>
        Requisitos().SingleOrDefaultAsync(x => x.IdRequisito == id, ct);
    public Task<Analise?> AnaliseAsync(int id, CancellationToken ct = default) =>
        Analises().SingleOrDefaultAsync(x => x.IdAnalise == id, ct);
    public Task<Sugestao?> SugestaoAsync(int id, CancellationToken ct = default) =>
        Sugestoes().SingleOrDefaultAsync(x => x.IdSugestao == id, ct);
    public Task<OperacaoAnalise?> OperacaoAsync(Guid id, CancellationToken ct = default) =>
        Operacoes().SingleOrDefaultAsync(x => x.IdOperacao == id, ct);
}

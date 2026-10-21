using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RequisitaPlus.Web.Models;

namespace RequisitaPlus.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>(options)
{
    public DbSet<Requisito> Requisitos => Set<Requisito>();
    public DbSet<Analise> Analises => Set<Analise>();
    public DbSet<Sugestao> Sugestoes => Set<Sugestao>();
    public DbSet<OperacaoAnalise> OperacoesAnalise => Set<OperacaoAnalise>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}

using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.Models;
using RequisitaPlus.Web.Services.Requisitos;
using RequisitaPlus.Tests.Support;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

public sealed class PilotOwnershipTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void OwnershipPredicatesSelectOnlyOwnGraphAndTranslateWithoutExecutingSql(int owner)
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer().EnableServiceProviderCaching(false).Options);
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = TestData.Principal(owner) } };
        var service = new AcessoProprietarioService(db, http);
        var data = new TestData();
        var requirements = Evaluate(service.Requisitos(), data.Requirements);
        var analyses = Evaluate(service.Analises(), data.Analyses);
        var suggestions = Evaluate(service.Sugestoes(), data.Suggestions);
        var operations = Evaluate(service.Operacoes(), data.Operations);
        Assert.Equal(owner, Assert.Single(requirements).IdUsuario);
        Assert.Equal(owner, Assert.Single(analyses).Requisito.IdUsuario);
        Assert.Equal(owner, Assert.Single(suggestions).Analise.Requisito.IdUsuario);
        Assert.Equal(owner, Assert.Single(operations).IdUsuario);
        foreach (var id in new[] { 3 - owner, 999 })
        {
            Assert.Null(requirements.SingleOrDefault(x => x.IdRequisito == id));
            Assert.Null(analyses.SingleOrDefault(x => x.IdAnalise == id));
            Assert.Null(suggestions.SingleOrDefault(x => x.IdSugestao == id));
            Assert.Null(operations.SingleOrDefault(x => x.IdRequisito == id));
        }
        Assert.Contains("WHERE", service.Requisitos().Where(x => x.IdRequisito == owner).ToQueryString());
        Assert.Contains("JOIN", service.Analises().ToQueryString());
        Assert.Contains("JOIN", service.Sugestoes().ToQueryString());
        Assert.Contains("IdUsuario", service.Operacoes().ToQueryString());
        data.Operations[owner - 1].Requisito = data.Requirements[2 - owner];
        Assert.Empty(Evaluate(service.Operacoes(), data.Operations));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("1", false)]
    [InlineData("invalid", true)]
    [InlineData("0", true)]
    [InlineData("-1", true)]
    public void MissingUnauthenticatedOrInvalidIdentityFailsClosed(string? id, bool authenticated)
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer().EnableServiceProviderCaching(false).Options);
        var identity = new ClaimsIdentity(id is null ? [] : [new Claim(ClaimTypes.NameIdentifier, id)], authenticated ? "Simulado" : null);
        var service = new AcessoProprietarioService(db, new HttpContextAccessor
        { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } });
        var data = new TestData();
        Assert.Empty(Evaluate(service.Requisitos(), data.Requirements));
        Assert.Empty(Evaluate(service.Analises(), data.Analyses));
        Assert.Empty(Evaluate(service.Sugestoes(), data.Suggestions));
        Assert.Empty(Evaluate(service.Operacoes(), data.Operations));
        Assert.Contains("WHERE", service.Requisitos().ToQueryString());
    }

    // Exercise the exact EF predicate on simulated objects, independently of SQL execution.
    // ToQueryString above separately verifies SQL Server translation, not database behavior.
    private static T[] Evaluate<T>(IQueryable<T> query, T[] values)
    {
        var source = values.AsQueryable();
        var expression = new RootReplacement(source.Expression).Visit(query.Expression)!;
        return source.Provider.CreateQuery<T>(expression).ToArray();
    }
    private sealed class RootReplacement(Expression source) : ExpressionVisitor
    {
        protected override Expression VisitExtension(Expression node) => node is EntityQueryRootExpression ? source : base.VisitExtension(node);
    }
}

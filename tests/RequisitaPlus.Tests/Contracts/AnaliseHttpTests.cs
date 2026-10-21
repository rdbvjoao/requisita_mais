using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RequisitaPlus.Tests.Support;
using RequisitaPlus.Tests.Unit;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.Services.IA;
using Xunit;

namespace RequisitaPlus.Tests.Contracts;

public sealed class AnaliseHttpTests
{
    [Fact]
    public async Task StaleConfirmationAndArchivedDirectPostsDoNotSend()
    {
        var store = new AnaliseSimulada(); var modelo = new OllamaIntegrationTests.ModeloSimulado();
        await using var app = Application(store, modelo); await Login(app);
        var page = await app.SendAsync("/Analises/Confirmar/1"); var input = Entrada(page);
        store.Requisitos[0].VersaoTextual++;
        var response = await app.SendAsync("/Analises/Iniciar/1", input);
        Assert.Equal(409, response.Status);
        Assert.Contains("Recarregar", response.Body);
        store.Requisitos[0].Status = "Arquivado";
        Assert.Equal(409, (await app.SendAsync("/Analises/Confirmar/1")).Status);
        Assert.Equal(409, (await app.SendAsync("/Analises/Iniciar/1", input)).Status);
        Assert.DoesNotContain("Analisar com IA", (await app.SendAsync("/Requisitos/Detalhes/1")).Body);
        Assert.Equal(200, (await app.SendAsync("/Analises/Historico/1")).Status);
        Assert.Equal(0, modelo.Calls); Assert.Empty(store.Operacoes);
    }

    private static IdentityHttpApplication Application(AnaliseSimulada store, OllamaIntegrationTests.ModeloSimulado modelo) => new(services =>
    {
        services.Replace(ServiceDescriptor.Scoped<ApplicationDbContext>(_ => store.Contexto()));
        services.Replace(ServiceDescriptor.Singleton<IOllamaClient>(modelo));
    });
    private static async Task Login(IdentityHttpApplication app, string email = "owner@example.invalid")
    {
        var page = await app.SendAsync("/Identity/Account/Register");
        Assert.Equal(302, (await app.SendAsync("/Identity/Account/Register", new()
        {
            ["__RequestVerificationToken"] = IdentityHttpApplication.Token(page), ["Input.Nome"] = "Analista",
            ["Input.Email"] = email, ["Input.Password"] = "Aa1!aaaa", ["Input.ConfirmPassword"] = "Aa1!aaaa"
        })).Status);
    }
    private static string Hidden(IdentityHttpApplication.Response page, string name) => WebUtility.HtmlDecode(
        Regex.Match(page.Body, "name=\"" + name + "\"[^>]*value=\"([^\"]*)\"").Groups[1].Value);
    private static Dictionary<string, string> Entrada(IdentityHttpApplication.Response page) => new()
    {
        ["__RequestVerificationToken"] = IdentityHttpApplication.Token(page),
        ["IdOperacao"] = Hidden(page, "IdOperacao"), ["Versao"] = Hidden(page, "Versao"),
        ["RowVersion"] = Hidden(page, "RowVersion"), ["Confirmacao"] = "true"
    };

    [Fact]
    public async Task AuthCsrfConfirmationAndOwnershipProtectInference()
    {
        var store = new AnaliseSimulada(); var modelo = new OllamaIntegrationTests.ModeloSimulado();
        await using var app = Application(store, modelo);
        Assert.Equal(302, (await app.SendAsync("/Analises/Confirmar/1")).Status);
        await Login(app);
        Assert.Equal(400, (await app.SendAsync("/Analises/Iniciar/1", [])).Status);
        var page = await app.SendAsync("/Analises/Confirmar/1");
        Assert.Equal(200, page.Status); Assert.Equal(0, modelo.Calls);
        var input = Entrada(page); input["Confirmacao"] = "false";
        Assert.Equal(200, (await app.SendAsync("/Analises/Iniciar/1", input)).Status);
        Assert.Equal(0, modelo.Calls);
        input["Confirmacao"] = "true";
        Assert.Equal(404, (await app.SendAsync("/Analises/Iniciar/999", input)).Status);
        await Login(app, "second@example.invalid");
        foreach (var action in new[] { "Confirmar/1", "Historico/1", "Detalhes/1", "Operacao/" + input["IdOperacao"] })
            Assert.Equal(404, (await app.SendAsync("/Analises/" + action)).Status);
        page = await app.SendAsync("/Requisitos/Criar"); input["__RequestVerificationToken"] = IdentityHttpApplication.Token(page);
        Assert.Equal(404, (await app.SendAsync("/Analises/Iniciar/1", input)).Status);
        Assert.Equal(0, modelo.Calls); Assert.Empty(store.Operacoes);
    }

    [Fact]
    public async Task BrowserFlowShowsResultAndHistoryWithoutReinferingOrApplyingSuggestions()
    {
        var store = new AnaliseSimulada(); var modelo = new OllamaIntegrationTests.ModeloSimulado();
        modelo.Responder = () => Task.FromResult(new RespostaIA(true, [new("Termo vago", "<script>alert('x')</script>", "<b>Proposta</b>")], 100, 60));
        await using var app = Application(store, modelo); await Login(app);
        var detalhes = await app.SendAsync("/Requisitos/Detalhes/1");
        Assert.Contains("Analisar com IA", detalhes.Body);
        Assert.Contains("Nenhuma", (await app.SendAsync("/Analises/Historico/1")).Body);
        var page = await app.SendAsync("/Analises/Confirmar/1"); var input = Entrada(page);
        input["IdUsuario"] = "2"; input["Status"] = "Revisado";
        var inicio = await app.SendAsync("/Analises/Iniciar/1", input);
        Assert.Equal(302, inicio.Status);
        var operacao = await app.SendAsync(inicio.Location); Assert.Equal(302, operacao.Status);
        var resultado = await app.SendAsync(operacao.Location);
        Assert.Equal(200, resultado.Status); Assert.Contains("Pendente", resultado.Body);
        Assert.DoesNotContain("<script>alert", resultado.Body); Assert.Contains("&lt;script&gt;", resultado.Body);
        Assert.Contains("&lt;b&gt;Proposta", resultado.Body);
        Assert.Contains("Análise", WebUtility.HtmlDecode((await app.SendAsync("/Analises/Historico/1")).Body));
        await app.SendAsync(operacao.Location); await app.SendAsync("/Analises/Historico/1");
        Assert.Equal(302, (await app.SendAsync("/Analises/Iniciar/1", input)).Status);
        Assert.Equal(1, modelo.Calls); Assert.Single(store.Analises);
        Assert.Equal(1, store.Operacoes.Single().IdUsuario); Assert.Equal("Analisado", store.Requisitos.Single().Status);
        Assert.Equal("Descrição original", store.Requisitos.Single().Descricao);
        await Login(app, "second@example.invalid");
        Assert.Equal(404, (await app.SendAsync(operacao.Location)).Status);
        Assert.Equal(404, (await app.SendAsync(inicio.Location)).Status);
        Assert.Equal(404, (await app.SendAsync("/Analises/Historico/1")).Status);

    }
}

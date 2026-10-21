using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RequisitaPlus.Tests.Support;
using RequisitaPlus.Web.Data;
using Xunit;

namespace RequisitaPlus.Tests.Contracts;

public sealed class RequisitoCrudHttpTests
{
    private static IdentityHttpApplication Application(SimulatedRequisitoStore store) => new(services =>
        services.Replace(ServiceDescriptor.Scoped<ApplicationDbContext>(_ => store.Context())));

    private static async Task Login(IdentityHttpApplication app, string email = "owner@example.invalid")
    {
        var page = await app.SendAsync("/Identity/Account/Register");
        Assert.Equal(302, (await app.SendAsync("/Identity/Account/Register", new()
        {
            ["__RequestVerificationToken"] = IdentityHttpApplication.Token(page), ["Input.Nome"] = "Analista",
            ["Input.Email"] = email, ["Input.Password"] = "Aa1!aaaa", ["Input.ConfirmPassword"] = "Aa1!aaaa"
        })).Status);
    }

    private static Dictionary<string, string> Input(IdentityHttpApplication.Response page) => new()
    {
        ["__RequestVerificationToken"] = IdentityHttpApplication.Token(page),
        ["Titulo"] = "Título cadastrado", ["Descricao"] = "Descrição completa", ["Prioridade"] = "Média"
    };

    private static string Hidden(IdentityHttpApplication.Response page, string name) => WebUtility.HtmlDecode(
        Regex.Match(page.Body, "name=\"" + name + "\"[^>]*value=\"([^\"]*)\"").Groups[1].Value);

    [Fact]
    public async Task DashboardGetPreservesFiltersRealStatusAndDetailsRouteWithoutWrites()
    {
        var store = new SimulatedRequisitoStore();
        store.Seed(1, 1, "Analisado"); store.Seed(2, 2, "Analisado"); store.Seed(3, 1, "Arquivado");
        store.Rows[0].Titulo = "Pagamento <cartão>";
        await using var app = Application(store);
        await Login(app);
        var page = await app.SendAsync("/Requisitos?Pesquisa=Pagamento&Prioridade=Alta&Status=Analisado");
        Assert.Equal(200, page.Status);
        Assert.Contains("method=\"get\"", page.Body);
        Assert.Contains("name=\"Pesquisa\" value=\"Pagamento\"", page.Body);
        Assert.Contains("value=\"Alta\" selected=\"selected\"", page.Body);
        Assert.Contains("value=\"Analisado\" selected=\"selected\"", page.Body);
        Assert.Contains("Pagamento &lt;cart", page.Body);
        Assert.Contains("/Requisitos/Detalhes/1", page.Body);
        Assert.DoesNotContain("/Requisitos/Detalhes/2", page.Body);
        Assert.DoesNotContain("/Requisitos/Detalhes/3", page.Body);
        Assert.Contains("Sugestões pendentes", WebUtility.HtmlDecode(page.Body));
        Assert.Contains("status-analyzed", page.Body);
        page = await app.SendAsync("/Requisitos?Status=Arquivado");
        Assert.Contains("/Requisitos/Detalhes/3", page.Body);
        page = await app.SendAsync("/Requisitos?Pesquisa=inexistente");
        Assert.Contains("Nenhum requisito corresponde aos filtros", page.Body);
        page = await app.SendAsync("/Requisitos?Prioridade=Urgente&Status=FalhaEncerrada");
        Assert.Contains("Selecione uma prioridade válida", WebUtility.HtmlDecode(page.Body));
        Assert.Contains("Selecione um status válido", WebUtility.HtmlDecode(page.Body));
        Assert.DoesNotContain("/Requisitos/Detalhes/1", page.Body);
        Assert.Equal(0, store.Writes);
    }

    [Theory]
    [InlineData("/Requisitos")]
    [InlineData("/Requisitos/Criar")]
    [InlineData("/Requisitos/Detalhes/1")]
    [InlineData("/Requisitos/Editar/1")]
    [InlineData("/Requisitos/Arquivados")]
    public async Task VisitorIsChallenged(string path)
    {
        await using var app = Application(new());
        var response = await app.SendAsync(path);
        Assert.Equal(302, response.Status);
        Assert.Contains("/Identity/Account/Login", response.Location);
    }

    [Theory]
    [InlineData("/Requisitos/Criar")]
    [InlineData("/Requisitos/Editar/1")]
    [InlineData("/Requisitos/Arquivar/1")]
    public async Task MissingOrForgedAntiforgeryNeverWrites(string path)
    {
        var store = new SimulatedRequisitoStore(); store.Seed();
        await using var app = Application(store);
        await Login(app);
        Assert.Equal(400, (await app.SendAsync(path, [])).Status);
        Assert.Equal(400, (await app.SendAsync(path, new() { ["__RequestVerificationToken"] = "forged" })).Status);
        Assert.Equal(0, store.Writes);
    }

    [Fact]
    public async Task CompleteCrudUsesServerIdentityVersioningAndLogicalDeletion()
    {
        var store = new SimulatedRequisitoStore();
        await using var app = Application(store);
        await Login(app);
        Assert.Contains("Nenhum requisito", (await app.SendAsync("/Requisitos")).Body);
        var page = await app.SendAsync("/Requisitos/Criar");
        Assert.Contains("value=\"\" selected=\"selected\"", page.Body);
        var input = Input(page);
        input["Titulo"] = "<script>alert('x')</script>  ";
        input["Descricao"] = "Descrição Unicode 😀\nCom espaços finais  ";
        var created = await app.SendAsync("/Requisitos/Criar", input);
        Assert.Equal(302, created.Status);
        var row = Assert.Single(store.Rows);
        Assert.Equal(1, row.IdUsuario);
        Assert.Equal("Rascunho", row.Status);
        Assert.Equal(1, row.VersaoTextual);
        Assert.Equal(0, row.UltimaOrdemAnalise);
        Assert.Equal(input["Titulo"], row.Titulo);
        Assert.Equal(input["Descricao"], row.Descricao);
        Assert.Equal(DateTimeKind.Utc, row.DataCadastroUtc.Kind);
        var detail = await app.SendAsync(created.Location);
        Assert.Contains("&lt;script&gt;", detail.Body);
        Assert.DoesNotContain("<script>alert", detail.Body);
        Assert.Contains("cadastrado com sucesso", detail.Body);

        page = await app.SendAsync("/Requisitos/Editar/1");
        input = Input(page);
        input["VersaoEsperada"] = Hidden(page, "VersaoEsperada");
        input["RowVersion"] = Hidden(page, "RowVersion");
        Assert.Equal(302, (await app.SendAsync("/Requisitos/Editar/1", input)).Status);
        Assert.Equal(2, store.Rows.Single().VersaoTextual);
        Assert.Equal(409, (await app.SendAsync("/Requisitos/Editar/1", input)).Status);
        Assert.Equal(2, store.Rows.Single().VersaoTextual);

        detail = await app.SendAsync("/Requisitos/Detalhes/1");
        var archive = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = IdentityHttpApplication.Token(detail), ["RowVersion"] = Hidden(detail, "RowVersion")
        };
        Assert.Equal(200, (await app.SendAsync("/Requisitos/Arquivar/1", archive)).Status);
        Assert.Equal("Rascunho", store.Rows.Single().Status); // Explicit confirmation required.
        archive["Confirmacao"] = "true";
        Assert.Equal(302, (await app.SendAsync("/Requisitos/Arquivar/1", archive)).Status);
        row = Assert.Single(store.Rows);
        Assert.Equal("Arquivado", row.Status);
        Assert.Equal(2, row.VersaoTextual);
        Assert.Contains("Nenhum requisito", (await app.SendAsync("/Requisitos")).Body);
        Assert.Contains("Título cadastrado", WebUtility.HtmlDecode((await app.SendAsync("/Requisitos/Arquivados")).Body));
        Assert.Equal(200, (await app.SendAsync("/Requisitos/Detalhes/1")).Status);
        Assert.Equal(409, (await app.SendAsync("/Requisitos/Editar/1")).Status);
        page = await app.SendAsync("/Requisitos/Criar");
        Assert.Equal(409, (await app.SendAsync("/Requisitos/Editar/1", Input(page))).Status);
        archive["__RequestVerificationToken"] = IdentityHttpApplication.Token(page);
        Assert.Equal(409, (await app.SendAsync("/Requisitos/Arquivar/1", archive)).Status);
        Assert.Equal(405, (await app.SendAsync("/Requisitos/Arquivar/1")).Status);
        Assert.Equal(404, (await app.SendAsync("/Requisitos/Excluir/1", archive)).Status);
        Assert.Equal(404, (await app.SendAsync("/Requisitos/Restaurar/1", archive)).Status);
        Assert.Equal(3, store.Writes);
    }

    [Theory]
    [InlineData("Titulo", " ")]
    [InlineData("Descricao", "\t\n")]
    [InlineData("Prioridade", "")]
    [InlineData("Prioridade", "alta")]
    [InlineData("Prioridade", "Urgente")]
    [InlineData("Status", "Revisado")]
    [InlineData("IdUsuario", "2")]
    [InlineData("OwnerId", "2")]
    public async Task InvalidOrOverpostedFieldsCannotCreateOrEdit(string field, string value)
    {
        var store = new SimulatedRequisitoStore(); store.Seed();
        await using var app = Application(store);
        await Login(app);
        foreach (var path in new[] { "/Requisitos/Criar", "/Requisitos/Editar/1" })
        {
            var page = await app.SendAsync(path);
            var input = Input(page);
            input["VersaoEsperada"] = Hidden(page, "VersaoEsperada");
            input["RowVersion"] = Hidden(page, "RowVersion");
            input[field] = value;
            var response = await app.SendAsync(path, input);
            Assert.Equal(200, response.Status);
            Assert.DoesNotContain("alert-success", response.Body);
        }
        Assert.Equal(0, store.Writes);
        Assert.Equal("Título original", Assert.Single(store.Rows).Titulo);
    }

    [Theory]
    [InlineData("Titulo", 151)]
    [InlineData("Descricao", 5001)]
    public async Task DirectPostsRejectExcessLength(string field, int size)
    {
        var store = new SimulatedRequisitoStore(); store.Seed();
        await using var app = Application(store); await Login(app);
        foreach (var path in new[] { "/Requisitos/Criar", "/Requisitos/Editar/1" })
        {
            var page = await app.SendAsync(path); var input = Input(page);
            input["VersaoEsperada"] = "1"; input["RowVersion"] = Hidden(page, "RowVersion");
            input[field] = new string('x', size);
            Assert.Equal(200, (await app.SendAsync(path, input)).Status);
        }
        Assert.Equal(0, store.Writes);
    }

    [Theory]
    [InlineData("VersaoEsperada", "0")]
    [InlineData("RowVersion", "")]
    [InlineData("RowVersion", "invalid")]
    [InlineData("RowVersion", "AQ==")]
    public async Task InvalidVersionDataIsRejectedByHttpValidation(string field, string value)
    {
        var store = new SimulatedRequisitoStore(); store.Seed();
        await using var app = Application(store); await Login(app);
        var page = await app.SendAsync("/Requisitos/Editar/1");
        var input = Input(page); input["VersaoEsperada"] = "1"; input["RowVersion"] = Hidden(page, "RowVersion");
        input[field] = value;
        Assert.Equal(200, (await app.SendAsync("/Requisitos/Editar/1", input)).Status);
        Assert.Equal(0, store.Writes);
    }

    [Fact]
    public async Task TwoUsersSeeOnlyOwnListsAndForeignIdsMatchMissingIds()
    {
        var store = new SimulatedRequisitoStore(); store.Seed(); store.Seed(2, 2); store.Seed(3, 2, "Arquivado");
        store.Rows[0].Titulo = "PRIVADO UM"; store.Rows[1].Titulo = "PRIVADO DOIS"; store.Rows[2].Titulo = "ARQUIVADO DOIS";
        await using var app = Application(store);
        await Login(app);
        Assert.DoesNotContain("PRIVADO DOIS", (await app.SendAsync("/Requisitos")).Body);
        Assert.DoesNotContain("ARQUIVADO DOIS", (await app.SendAsync("/Requisitos/Arquivados")).Body);
        foreach (var action in new[] { "Detalhes", "Editar" })
        {
            var foreign = await app.SendAsync($"/Requisitos/{action}/2");
            var missing = await app.SendAsync($"/Requisitos/{action}/999");
            Assert.Equal(404, foreign.Status); Assert.Equal(missing.Body, foreign.Body);
        }
        var page = await app.SendAsync("/Requisitos/Criar");
        foreach (var action in new[] { "Editar", "Arquivar" })
        {
            var foreign = await app.SendAsync($"/Requisitos/{action}/2", Input(page));
            Assert.Equal(404, foreign.Status); Assert.Equal("Requisito não encontrado", foreign.Body);
        }
        await Login(app, "second@example.invalid");
        var list = await app.SendAsync("/Requisitos");
        Assert.Contains("PRIVADO DOIS", list.Body); Assert.DoesNotContain("PRIVADO UM", list.Body);
        Assert.Contains("ARQUIVADO DOIS", (await app.SendAsync("/Requisitos/Arquivados")).Body);
        Assert.Equal(404, (await app.SendAsync("/Requisitos/Detalhes/1")).Status);
        Assert.Equal(0, store.Writes);
    }

    [Theory]
    [InlineData("Criar")]
    [InlineData("Editar/1")]
    [InlineData("Arquivar/1")]
    public async Task SaveFailurePreservesPersistedDataAndDoesNotExposeProviderMessage(string action)
    {
        var store = new SimulatedRequisitoStore(); store.Seed();
        store.SaveFailure = new DbUpdateException("SIMULADO SQL secret connection and user text");
        await using var app = Application(store); await Login(app);
        var page = await app.SendAsync(action == "Arquivar/1" ? "/Requisitos/Detalhes/1" : "/Requisitos/" + action);
        var input = Input(page); input["VersaoEsperada"] = "1"; input["RowVersion"] = Hidden(page, "RowVersion"); input["Confirmacao"] = "true";
        var response = await app.SendAsync("/Requisitos/" + action, input);
        Assert.Equal(200, response.Status);
        Assert.Contains("Não foi possível salvar", WebUtility.HtmlDecode(response.Body));
        Assert.DoesNotContain("secret connection", response.Body);
        Assert.DoesNotContain("alert-success", response.Body);
        Assert.Equal("Título original", Assert.Single(store.Rows).Titulo);
        Assert.Equal("Rascunho", store.Rows.Single().Status);
        Assert.Equal(0, store.Writes);
    }
}

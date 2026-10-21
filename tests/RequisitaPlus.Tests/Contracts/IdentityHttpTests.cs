using Microsoft.AspNetCore.Identity;
using RequisitaPlus.Tests.Support;
using RequisitaPlus.Web.Models;
using Xunit;

namespace RequisitaPlus.Tests.Contracts;

public sealed class IdentityHttpTests
{
    private const string Register = "/Identity/Account/Register";
    private const string Login = "/Identity/Account/Login";
    private const string Logout = "/Identity/Account/Logout";
    private static Dictionary<string, string> Registration(string token, string email = "analista@example.invalid") => new()
    {
        ["__RequestVerificationToken"] = token,
        ["Input.Nome"] = "Analista",
        ["Input.Email"] = email,
        ["Input.Password"] = "Aa1!aaaa",
        ["Input.ConfirmPassword"] = "Aa1!aaaa"
    };

    [Theory]
    [InlineData(Register)]
    [InlineData(Login)]
    public async Task PublicFormsHaveWorkingNavigationAndAntiforgery(string path)
    {
        await using var app = new IdentityHttpApplication();
        var response = await app.SendAsync(path);
        Assert.Equal(200, response.Status);
        Assert.NotEmpty(IdentityHttpApplication.Token(response));
        Assert.Contains(Register, response.Body);
        Assert.Contains(Login, response.Body);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Privacy")]
    [InlineData("/Identity/Account/Manage")]
    [InlineData("/Identity/Account/ForgotPassword")]
    [InlineData(Logout)]
    public async Task DirectProtectedAccessChallengesVisitor(string path)
    {
        await using var app = new IdentityHttpApplication();
        var response = await app.SendAsync(path);
        Assert.Equal(302, response.Status);
        Assert.Contains(Login, response.Location);
    }

    [Theory]
    [InlineData(Register)]
    [InlineData(Login)]
    public async Task MissingAndTamperedAntiforgeryRejectPosts(string path)
    {
        await using var app = new IdentityHttpApplication();
        Assert.Equal(400, (await app.SendAsync(path, [])).Status);
        await app.SendAsync(path);
        Assert.Equal(400, (await app.SendAsync(path, Registration("invalid"))).Status);
        Assert.Empty(app.Store.Users);
    }

    [Fact]
    public async Task RegisterLoginWrongPasswordAndPostLogoutExerciseRealIdentityPipeline()
    {
        await using var app = new IdentityHttpApplication();
        var form = await app.SendAsync(Register);
        var created = await app.SendAsync(Register, Registration(IdentityHttpApplication.Token(form)));
        Assert.Equal(302, created.Status);
        var user = Assert.Single(app.Store.Users);
        Assert.NotEqual("Aa1!aaaa", user.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, new PasswordHasher<ApplicationUser>().VerifyHashedPassword(user, user.PasswordHash!, "Aa1!aaaa"));
        Assert.Equal(200, (await app.SendAsync("/")).Status);
        var confirmation = await app.SendAsync(Logout);
        Assert.Equal(200, confirmation.Status); // GET does not sign out.
        Assert.Equal(200, (await app.SendAsync("/")).Status);
        Assert.Equal(400, (await app.SendAsync(Logout, [])).Status);
        Assert.Equal(400, (await app.SendAsync(Logout, new() { ["__RequestVerificationToken"] = "invalid" })).Status);
        Assert.Equal(200, (await app.SendAsync("/")).Status);
        Assert.Equal(302, (await app.SendAsync(Logout, new() { ["__RequestVerificationToken"] = IdentityHttpApplication.Token(confirmation) })).Status);
        Assert.Equal(302, (await app.SendAsync("/")).Status);
        var loginForm = await app.SendAsync(Login);
        var login = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = IdentityHttpApplication.Token(loginForm),
            ["Input.Email"] = "ANALISTA@EXAMPLE.INVALID",
            ["Input.Password"] = "wrong"
        };
        var wrong = await app.SendAsync(Login, login);
        Assert.Equal(200, wrong.Status);
        Assert.Contains("Credenciais inv", System.Net.WebUtility.HtmlDecode(wrong.Body));
        Assert.Equal(302, (await app.SendAsync("/")).Status);
        login["Input.Password"] = "Aa1!aaaa";
        Assert.Equal(302, (await app.SendAsync(Login, login)).Status);
        var home = await app.SendAsync("/");
        Assert.Equal(200, home.Status);
        Assert.Contains(Logout, home.Body);
        Assert.Contains("method=\"post\"", home.Body);
    }

    [Fact]
    public async Task InvalidAndNormalizedDuplicateRegistrationNeverReportSuccess()
    {
        await using var app = new IdentityHttpApplication();
        var page = await app.SendAsync(Register);
        var input = Registration(IdentityHttpApplication.Token(page));
        input["Input.Nome"] = new string('x', 101);
        Assert.Equal(200, (await app.SendAsync(Register, input)).Status);
        Assert.Empty(app.Store.Users);
        input["Input.Nome"] = "Analista";
        Assert.Equal(302, (await app.SendAsync(Register, input)).Status);
        page = await app.SendAsync(Register);
        var duplicate = await app.SendAsync(Register, Registration(IdentityHttpApplication.Token(page), "ANALISTA@EXAMPLE.INVALID"));
        Assert.Equal(200, duplicate.Status);
        Assert.Single(app.Store.Users);
    }

    [Theory]
    [InlineData("Input.Nome", " ")]
    [InlineData("Input.Email", "invalid")]
    [InlineData("Input.Email", "a b@example.invalid")]
    [InlineData("Input.Password", "Aa1!aaa")]
    [InlineData("Input.Password", "aa1!aaaa")]
    [InlineData("Input.Password", "AA1!AAAA")]
    [InlineData("Input.Password", "AaA!aaaa")]
    [InlineData("Input.Password", "Aa11aaaa")]
    [InlineData("Input.ConfirmPassword", "different")]
    public async Task DirectRegistrationPostCannotBypassServerValidation(string field, string value)
    {
        await using var app = new IdentityHttpApplication();
        var page = await app.SendAsync(Register);
        var input = Registration(IdentityHttpApplication.Token(page));
        input[field] = value;
        if (field == "Input.Password") input["Input.ConfirmPassword"] = value;
        var response = await app.SendAsync(Register, input);
        Assert.Equal(200, response.Status);
        Assert.Empty(app.Store.Users);
        Assert.Equal(302, (await app.SendAsync("/")).Status);
    }

    [Fact]
    public async Task UnknownEmailReceivesSameCredentialErrorAsWrongPassword()
    {
        await using var app = new IdentityHttpApplication();
        var page = await app.SendAsync(Login);
        var response = await app.SendAsync(Login, new()
        {
            ["__RequestVerificationToken"] = IdentityHttpApplication.Token(page),
            ["Input.Email"] = "unknown@example.invalid",
            ["Input.Password"] = "Aa1!aaaa"
        });
        Assert.Equal(200, response.Status);
        Assert.Contains("Credenciais inv", System.Net.WebUtility.HtmlDecode(response.Body));
        Assert.Equal(302, (await app.SendAsync("/")).Status);
    }

    [Theory]
    [InlineData(Register)]
    [InlineData(Login)]
    public async Task ExternalReturnUrlCannotRedirectOutsideApplication(string path)
    {
        await using var app = new IdentityHttpApplication();
        var form = await app.SendAsync(Register);
        await app.SendAsync(Register, Registration(IdentityHttpApplication.Token(form)));
        form = await app.SendAsync(path);
        var input = path == Register ? Registration(IdentityHttpApplication.Token(form), "second@example.invalid") : new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = IdentityHttpApplication.Token(form),
            ["Input.Email"] = "analista@example.invalid",
            ["Input.Password"] = "Aa1!aaaa"
        };
        var result = await app.SendAsync(path + "?returnUrl=https%3A%2F%2Fexample.invalid", input);
        Assert.Equal(302, result.Status);
        Assert.Equal("/", result.Location);
    }
}

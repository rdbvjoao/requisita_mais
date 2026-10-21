using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RequisitaPlus.Tests.Support;
using RequisitaPlus.Web.Areas.Identity.Pages.Account;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.Models;
using Xunit;

namespace RequisitaPlus.Tests.Integration.SqlServer;

[Trait("Category", "SqlServer")]
[Collection("SqlServer")]
public sealed class IdentityUniquenessTests
{
    [SqlServerFact]
    public Task ConcurrentRegistrationsWithSameNormalizedEmailHaveExactlyOneSuccess() => SqlSafety.ProtectAsync(async () =>
    {
        await using var fixture = new SqlServerFixture();
        await fixture.OpenForTestsAsync();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "Testing", ApplicationName = typeof(Program).Assembly.GetName().Name });
        Program.ConfigureServices(builder.Services, new ConfigurationBuilder().Build());
        builder.Logging.ClearProviders(); // No raw exception or connection diagnostics in test sinks.
        builder.Services.AddScoped(_ => fixture.CreateContext());
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        var barrier = new RegistrationBarrier();
        builder.Services.AddSingleton<IUserValidator<ApplicationUser>>(barrier);
        await using var provider = builder.Services.BuildServiceProvider();
        var email = $"simulado-concorrencia-{Guid.NewGuid():N}@example.invalid";

        async Task<(IActionResult Result, bool Error, bool Cookie, string[] Messages, bool Authenticated)> RegisterAsync(string inputEmail)
        {
            await using var scope = provider.CreateAsyncScope();
            var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            http.Request.Scheme = "https";
            scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = http;
            var page = new RegisterModel(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
                scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>())
            {
                PageContext = new PageContext { HttpContext = http },
                Url = new UrlHelper(new ActionContext(http, new RouteData(), new ActionDescriptor())),
                Input = new RegisterModel.InputModel
                { Nome = "SIMULADO concorrência", Email = inputEmail, Password = "Aa1!aaaa", ConfirmPassword = "Aa1!aaaa" }
            };
            try
            {
                var result = await page.OnPostAsync();
                var authenticated = (await http.AuthenticateAsync(IdentityConstants.ApplicationScheme)).Succeeded
                    || http.User.Identities.Any(x => x.IsAuthenticated);
                var messages = page.ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).ToArray();
                Assert.All(page.ModelState.Values.SelectMany(x => x.Errors), x => Assert.True(x.Exception == null, "Public validation must not contain exceptions."));
                return (result, !page.ModelState.IsValid, http.Response.Headers.SetCookie.Any(), messages, authenticated);
            }
            finally { scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = null; }
        }

        // Both requests finish Identity's duplicate pre-check before either INSERT.
        var results = await Task.WhenAll(RegisterAsync(email), RegisterAsync(email.ToUpperInvariant()));
        var success = Assert.Single(results, x => x.Result is LocalRedirectResult);
        Assert.False(success.Error);
        Assert.True(success.Cookie);
        Assert.Empty(success.Messages);
        var rejected = Assert.Single(results, x => x.Result is PageResult);
        Assert.True(rejected.Error);
        Assert.False(rejected.Cookie);
        Assert.False(rejected.Authenticated);
        Assert.True(rejected.Messages.Length == 1 && rejected.Messages[0] == "Este e-mail já está cadastrado.", "Public duplicate message mismatch.");
        Assert.All(rejected.Messages, message =>
        {
            foreach (var forbidden in new[] { "SqlException", "EmailIndex", "UserNameIndex", "2601", "2627", email, "Aa1!aaaa" })
                Assert.True(!message.Contains(forbidden, StringComparison.OrdinalIgnoreCase), "Public message contains private diagnostics.");
        });
        await using var verify = fixture.CreateContext();
        var normalized = new UpperInvariantLookupNormalizer().NormalizeEmail(email);
        Assert.Equal(1, await verify.Users.CountAsync(x => x.NormalizedEmail == normalized));
        var stored = await verify.Users.SingleAsync(x => x.NormalizedEmail == normalized);
        Assert.NotNull(stored.PasswordHash);
        Assert.Equal(1, await verify.Users.CountAsync(x => x.NormalizedUserName == normalized));
        // Preserve the synthetic row for Phase B review; no database deletion/cleanup.
    });

    private sealed class RegistrationBarrier : IUserValidator<ApplicationUser>
    {
        private int arrivals;
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            if (Interlocked.Increment(ref arrivals) == 2) release.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(20));
            return IdentityResult.Success;
        }
    }
}

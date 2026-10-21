using System.Security.Claims;
using System.Reflection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.Models;
using RequisitaPlus.Tests.Support;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

// Component tests: no HTTP host, SQL connection or real sign-in is started.
public sealed class PilotSecurityTests
{
    internal static ServiceProvider Services(Dictionary<string, string?>? settings = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "Testing", ApplicationName = typeof(Program).Assembly.GetName().Name });
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings ?? []).Build();
        Program.ConfigureServices(builder.Services, configuration);
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        return builder.Services.BuildServiceProvider();
    }

    [Fact]
    public async Task IdentityCookieCanBeIssuedValidatedTamperedAndDeletedWithoutSql()
    {
        using var services = Services();
        string cookie;
        using (var scope = services.CreateScope())
        {
            var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            context.Request.Scheme = "https";
            await context.SignInAsync(IdentityConstants.ApplicationScheme, TestData.Principal(1));
            var header = context.Response.Headers.SetCookie.ToString();
            Assert.Contains("httponly", header.ToLowerInvariant());
            Assert.Contains("secure", header.ToLowerInvariant());
            cookie = header.Split(';')[0];
        }
        using (var scope = services.CreateScope())
        {
            var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            context.Request.Headers.Cookie = cookie;
            var result = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            Assert.True(result.Succeeded);
            Assert.Equal("1", result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier));
            await context.SignOutAsync(IdentityConstants.ApplicationScheme);
            Assert.Contains("expires=Thu, 01 Jan 1970", context.Response.Headers.SetCookie.ToString());
        }
        using (var scope = services.CreateScope())
        {
            var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            context.Request.Headers.Cookie = cookie.Split('=')[0] + "=SIMULADO-inválido";
            Assert.False((await context.AuthenticateAsync(IdentityConstants.ApplicationScheme)).Succeeded);
        }
    }

    [Fact]
    public async Task AntiforgeryRejectsMissingAndTamperedTokensAndAcceptsValidPair()
    {
        using var services = Services();
        using var scope = services.CreateScope();
        var antiforgery = scope.ServiceProvider.GetRequiredService<IAntiforgery>();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Scheme = "https";
        context.Request.Method = "POST";
        await Assert.ThrowsAsync<AntiforgeryValidationException>(() => antiforgery.ValidateRequestAsync(context));
        var tokens = antiforgery.GetAndStoreTokens(context);
        var cookie = context.Response.Headers.SetCookie.ToString().Split(';')[0];
        var post = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        post.Request.Scheme = "https";
        post.Request.Method = "POST";
        post.Request.Headers.Cookie = cookie;
        post.Request.Headers[tokens.HeaderName!] = tokens.RequestToken;
        await antiforgery.ValidateRequestAsync(post);
        post.Request.Headers[tokens.HeaderName!] = "SIMULADO-token-inválido";
        // A new context avoids the per-request validation cache.
        var tampered = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        tampered.Request.Method = "POST";
        tampered.Request.Headers.Cookie = cookie;
        tampered.Request.Headers[tokens.HeaderName!] = "SIMULADO-token-inválido";
        await Assert.ThrowsAsync<AntiforgeryValidationException>(() => antiforgery.ValidateRequestAsync(tampered));
    }

    [Theory]
    [InlineData("Aa1!aaaa", true)]
    [InlineData("Aa1!aaa", false)]
    [InlineData("aa1!aaaa", false)]
    [InlineData("AA1!AAAA", false)]
    [InlineData("AaA!aaaa", false)]
    [InlineData("Aa11aaaa", false)]
    public async Task IdentityPasswordValidatorEnforcesApprovedPolicy(string password, bool valid)
    {
        using var services = Services();
        using var scope = services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var result = await new PasswordValidator<ApplicationUser>().ValidateAsync(manager, new ApplicationUser(), password);
        Assert.Equal(valid, result.Succeeded);
    }

    [Fact]
    public void FrameworkHashingUsesSaltAndVerifiesWithoutDatabase()
    {
        using var services = Services();
        using var scope = services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        var user = new ApplicationUser();
        var first = hasher.HashPassword(user, "Aa1!aaaa");
        Assert.NotEqual(first, hasher.HashPassword(user, "Aa1!aaaa"));
        Assert.DoesNotContain("Aa1!aaaa", first);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, first, "Aa1!aaaa"));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, first, "incorrect"));
    }

    [Fact]
    public async Task CookieIdentityAndFallbackAuthorizationAreConfigured()
    {
        using var services = Services();
        var options = services.GetRequiredService<IOptions<IdentityOptions>>().Value;
        Assert.True(options.User.RequireUniqueEmail);
        Assert.False(options.SignIn.RequireConfirmedEmail);
        Assert.False(options.SignIn.RequireConfirmedAccount);
        var cookie = services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentityConstants.ApplicationScheme);
        Assert.True(cookie.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, cookie.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Lax, cookie.Cookie.SameSite);
        Assert.Equal("/Identity/Account/Login", cookie.LoginPath.Value);
        var schemes = await services.GetRequiredService<IAuthenticationSchemeProvider>().GetDefaultAuthenticateSchemeAsync();
        Assert.Equal(IdentityConstants.ApplicationScheme, schemes!.Name);
        using var scope = services.CreateScope();
        var policy = await scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>().GetFallbackPolicyAsync();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        Assert.False((await auth.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, policy!)).Succeeded);
        Assert.True((await auth.AuthorizeAsync(TestData.Principal(1), null, policy!)).Succeeded);
        Assert.Contains(services.GetRequiredService<IOptions<MvcOptions>>().Value.Filters,
            filter => filter is AutoValidateAntiforgeryTokenAttribute);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var core = db.GetService<IDbContextOptions>().Extensions.OfType<CoreOptionsExtension>().Single();
        Assert.False(core.IsSensitiveDataLoggingEnabled);
        Assert.False(core.DetailedErrorsEnabled);
    }

    [Theory]
    [InlineData("/Account/Login", true)]
    [InlineData("/Account/Register", true)]
    [InlineData("/Account/ForgotPassword", false)]
    [InlineData("/Account/RegisterConfirmation", false)]
    [InlineData("/Account/Logout", false)]
    public void OnlyApprovedIdentityAccessPagesKeepAnonymousMetadata(string path, bool anonymous)
    {
        var descriptor = new PageActionDescriptor { AreaName = "Identity", ViewEnginePath = path };
        var model = new PageApplicationModel(descriptor, typeof(PilotSecurityTests).GetTypeInfo(), []);
        model.EndpointMetadata.Add(new AllowAnonymousAttribute());
        model.Filters.Add(new AllowAnonymousFilter());
        new IdentityAccessConvention().Apply(model);
        Assert.Equal(anonymous, model.EndpointMetadata.OfType<IAllowAnonymous>().Any());
        Assert.Equal(anonymous, model.Filters.OfType<IAllowAnonymousFilter>().Any());
    }

    [Fact]
    public async Task ActualDefaultIdentityPageDescriptorsPreserveOnlyApprovedPublicAccessAndPostProtection()
    {
        using var services = Services();
        var descriptors = services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items
            .OfType<PageActionDescriptor>().Where(x => x.AreaName == "Identity").ToArray();
        Assert.NotEmpty(descriptors);
        var loader = services.GetRequiredService<PageLoader>();
        foreach (var descriptor in descriptors)
        {
            var page = await loader.LoadAsync(descriptor, EndpointMetadataCollection.Empty);
            var endpoint = new RouteEndpointBuilder(_ => Task.CompletedTask, RoutePatternFactory.Parse("/simulado"), 0);
            foreach (var metadata in page.EndpointMetadata) endpoint.Metadata.Add(metadata);
            endpoint.Metadata.Add(page);
            Program.RestrictIdentityAnonymous(endpoint);
            var isPublic = endpoint.Metadata.OfType<IAllowAnonymous>().Any()
                || page.FilterDescriptors.Any(x => x.Filter is IAllowAnonymousFilter);
            Assert.True((descriptor.ViewEnginePath is "/Account/Login" or "/Account/Register") == isPublic,
                $"Unexpected anonymous metadata: {descriptor.ViewEnginePath}; metadata={string.Join(',', page.EndpointMetadata.Select(x => x.GetType().Name))}; filters={string.Join(',', page.FilterDescriptors.Select(x => x.Filter.GetType().Name))}");
            Assert.Contains(page.FilterDescriptors, x => x.Filter.GetType().Name.Contains("AutoValidateAntiforgeryToken"));
            if (descriptor.ViewEnginePath == "/Account/Logout")
                Assert.Contains(page.HandlerMethods, x => string.Equals(x.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase));
        }
    }
}

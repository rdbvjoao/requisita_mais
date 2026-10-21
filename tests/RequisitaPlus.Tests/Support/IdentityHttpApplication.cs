using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using RequisitaPlus.Web.Models;
using RequisitaPlus.Web.Data;

namespace RequisitaPlus.Tests.Support;

/// <summary>Runs the MVC/Razor HTTP pipeline in memory, with no server, socket or SQL store.</summary>
public sealed class IdentityHttpApplication : IAsyncDisposable
{
    private readonly WebApplication app;
    private readonly RequestDelegate pipeline;
    private readonly Dictionary<string, string> cookies = [];
    public SimulatedIdentityStore Store { get; } = new();

    public IdentityHttpApplication(Action<IServiceCollection>? configure = null)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/RequisitaPlus.Web"));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ContentRootPath = root,
            ApplicationName = typeof(Program).Assembly.GetName().Name
        });
        Program.ConfigureServices(builder.Services, new ConfigurationBuilder().Build());
        builder.Services.Replace(ServiceDescriptor.Scoped(_ => new ApplicationDbContextFactory().CreateDbContext([])));
        builder.Services.Replace(ServiceDescriptor.Singleton<IUserStore<ApplicationUser>>(Store));
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        configure?.Invoke(builder.Services);
        app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        // Explicit endpoint middleware allows direct pipeline execution without StartAsync.
#pragma warning disable ASP0014
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            endpoints.MapRazorPages().Finally(Program.RestrictIdentityAnonymous);
        });
#pragma warning restore ASP0014
        pipeline = ((IApplicationBuilder)app).Build();
    }

    public async Task<Response> SendAsync(string path, Dictionary<string, string>? form = null, string method = "GET")
    {
        using var scope = app.Services.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        var uri = new Uri("https://localhost" + path);
        context.Request.Path = uri.AbsolutePath;
        context.Request.QueryString = new QueryString(uri.Query);
        context.Request.Method = form == null ? method : "POST";
        context.Request.Headers.Cookie = string.Join("; ", cookies.Select(x => x.Key + "=" + x.Value));
        if (form != null)
        {
            var content = new FormUrlEncodedContent(form);
            var bytes = await content.ReadAsByteArrayAsync();
            context.Request.ContentType = "application/x-www-form-urlencoded";
            context.Request.ContentLength = bytes.Length;
            context.Request.Body = new MemoryStream(bytes);
        }
        context.Response.Body = new MemoryStream();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = context;
        try { await pipeline(context); }
        finally { accessor.HttpContext = null; }
        foreach (var header in context.Response.Headers.SetCookie)
        {
            var pair = header!.Split(';')[0].Split('=', 2);
            if (pair[1].Length == 0) cookies.Remove(pair[0]); else cookies[pair[0]] = pair[1];
        }
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        return new Response(context.Response.StatusCode, body, context.Response.Headers.Location.ToString());
    }

    public static string Token(Response response) => WebUtility.HtmlDecode(Regex.Match(response.Body,
        "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    public ValueTask DisposeAsync() => app.DisposeAsync();
    public sealed record Response(int Status, string Body, string Location);
}

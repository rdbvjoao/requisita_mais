using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.Models;
using RequisitaPlus.Web.Services;
using RequisitaPlus.Web.Services.Requisitos;
using RequisitaPlus.Web.Services.IA;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder(args);
Program.ConfigureServices(builder.Services, builder.Configuration);
// Exception messages can contain user data; record only our fixed event below.
Program.ConfigureLogging(builder.Logging);
var app = builder.Build();
app.UseExceptionHandler(error => error.Run(Program.HandleErrorAsync));
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets().AllowAnonymous();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.MapRazorPages().Finally(Program.RestrictIdentityAnonymous);
app.Run();

public partial class Program
{
    public static void RestrictIdentityAnonymous(EndpointBuilder endpoint)
    {
        var page = endpoint.Metadata.OfType<PageActionDescriptor>().LastOrDefault();
        if (page?.AreaName != "Identity" || page.ViewEnginePath is "/Account/Login" or "/Account/Register") return;
        // PageLoader adds handler attributes after application-model conventions.
        // Final endpoint metadata must enforce the same public-access boundary.
        for (var i = endpoint.Metadata.Count - 1; i >= 0; i--)
            if (endpoint.Metadata[i] is IAllowAnonymous) endpoint.Metadata.RemoveAt(i);
    }
    public static void ConfigureLogging(ILoggingBuilder logging)
    {
        logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);
        // EF logs provider exception text even when sensitive parameter logging is off.
        // Our fixed diagnostics below retain only numeric code and safe category.
        logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
        logging.Services.Configure<SimpleConsoleFormatterOptions>(options =>
        {
            options.UseUtcTimestamp = true;
            options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z' ";
        });
    }

    public static async Task HandleErrorAsync(HttpContext context)
    {
        var failure = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        if (failure != null && SqlErrorDiagnostics.Code(failure) is int code)
            SqlErrorDiagnostics.Log(context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RequisitaPlus.Sql"), code);
        context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RequisitaPlus.Erros")
            .LogError(new EventId(1000, "FalhaNaoTratada"), "Não foi possível concluir a solicitação.");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync("Não foi possível concluir a solicitação.");
    }
    // Tests inspect registration without starting a host or connecting to SQL.
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
        services.AddRazorPages(options => options.Conventions.Add(new IdentityAccessConvention()));
        services.AddDbContext<ApplicationDbContext>(options => options
            .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            .EnableSensitiveDataLogging(false).EnableDetailedErrors(false));
        services.AddDefaultIdentity<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = false;
        }).AddEntityFrameworkStores<ApplicationDbContext>();
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.LoginPath = "/Identity/Account/Login";
            options.LogoutPath = "/Identity/Account/Logout";
        });
        services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().Build());
        services.AddHttpContextAccessor();
        services.AddScoped<AcessoProprietarioService>();
        services.AddScoped<CadastroRequisitoService>();
        services.AddScoped<ConsultaRequisitoService>();
        services.AddScoped<EdicaoRequisitoService>();
        services.AddScoped<ArquivamentoRequisitoService>();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<RelogioLocal>();
        // No ValidateOnStart: invalid AI settings must not prevent Web/Identity registration.
        services.AddOptions<OllamaOptions>().Bind(configuration.GetSection(OllamaOptions.SectionName));
        services.AddSingleton<IValidateOptions<OllamaOptions>, OllamaOptionsValidator>();
        services.AddTransient<OllamaDisponibilidade>();
        services.AddSingleton<AnaliseService>();
        services.AddHttpClient<IOllamaClient, OllamaClient>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false })
            .RemoveAllLoggers();
    }
}

// Default UI includes anonymous recovery/confirmation pages that are not approved MVP access flows.
public sealed class IdentityAccessConvention : IPageApplicationModelConvention
{
    public void Apply(PageApplicationModel model)
    {
        if (model.AreaName != "Identity" || model.ViewEnginePath is "/Account/Login" or "/Account/Register") return;
        for (var i = model.Filters.Count - 1; i >= 0; i--)
            if (model.Filters[i] is IAllowAnonymousFilter) model.Filters.RemoveAt(i);
        for (var i = model.EndpointMetadata.Count - 1; i >= 0; i--)
            if (model.EndpointMetadata[i] is IAllowAnonymous) model.EndpointMetadata.RemoveAt(i);
    }
}

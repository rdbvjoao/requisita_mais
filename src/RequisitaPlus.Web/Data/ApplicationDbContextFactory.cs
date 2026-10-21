using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace RequisitaPlus.Web.Data;

/// <summary>Offline model generation only; never reads runtime secrets or opens a connection.</summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    // Identity UI configures auxiliary key lengths through DI. Reuse the same
    // registrations so design-time and runtime models agree; no host is built.
    private static readonly Lazy<ServiceProvider> DesignServices = new(() =>
    {
        var services = new ServiceCollection();
        Program.ConfigureServices(services, new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    });

    public ApplicationDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer("Server=design-time.invalid;Database=RequisitaPlus_DesignOnly;Integrated Security=true;Connect Timeout=1")
        .UseApplicationServiceProvider(DesignServices.Value)
        .AddInterceptors(new OfflineConnectionGuard())
        .Options);

    private sealed class OfflineConnectionGuard : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result) =>
            throw new InvalidOperationException("Design-time is offline: database connections are prohibited.");

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Design-time is offline: database connections are prohibited.");
    }
}

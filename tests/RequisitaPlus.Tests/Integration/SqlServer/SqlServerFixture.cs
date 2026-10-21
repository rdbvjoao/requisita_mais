using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RequisitaPlus.Tests.Support;
using RequisitaPlus.Web.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace RequisitaPlus.Tests.Integration.SqlServer;

[CollectionDefinition("SqlServer", DisableParallelization = true)]
public sealed class SqlServerCollection;

/// <summary>Construction is offline. Future sessions access only an existing authorized target.</summary>
public sealed class SqlServerFixture : IAsyncDisposable
{
    public const string Migration = "20261019220059_InitialCreate";
    public const string MigrationApproval = "REQUISITAPLUS_TEST_SQL_APPROVED_MIGRATION";
    private readonly Func<string, string?> read;
    private readonly string connection;
    private readonly string database;
    private SqlConnection? lease;
    private bool verified;
    private bool testReady;
    private int sessionClaimed;
    private readonly SemaphoreSlim leaseGuard = new(1, 1);
    private static readonly Lazy<ServiceProvider> ModelServices = new(() =>
    {
        var services = new ServiceCollection();
        Program.ConfigureServices(services, new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    });

    public SqlServerFixture() : this(Environment.GetEnvironmentVariable) { }
    public SqlServerFixture(Func<string, string?> read)
    {
        this.read = read;
        connection = IntegrationGate.RequireSqlConnectionString(read);
        database = new SqlConnectionStringBuilder(connection).InitialCatalog;
    }

    private void RequireConfiguration(string operation)
    {
        if (IntegrationGate.RequireSqlConnectionString(read) != connection || read(IntegrationGate.SqlOperation) != operation)
            throw new InvalidOperationException("SQL_CONFIGURATION_CHANGED_OR_OPERATION_REJECTED");
    }

    public ApplicationDbContext CreateContext()
    {
        if (!testReady) throw new InvalidOperationException("SQL_VERIFIED_SCHEMA_REQUIRED");
        return BuildContext();
    }

    private ApplicationDbContext BuildContext()
    {
        RequireConfiguration("test");
        if (!verified || lease?.State != ConnectionState.Open) throw new InvalidOperationException("SQL_VERIFIED_SESSION_REQUIRED");
        return new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection)
            .UseApplicationServiceProvider(ModelServices.Value).UseLoggerFactory(NullLoggerFactory.Instance)
            .EnableSensitiveDataLogging(false).EnableDetailedErrors(false)
            .AddInterceptors(new SessionGuard(this)).Options);
    }

    // Tests never apply DDL. Database provisioning is an independent human operation.
    public Task OpenForTestsAsync() => SqlSafety.ProtectAsync(async () =>
    {
        RequireConfiguration("test");
        try
        {
            await OpenLeaseAsync("test");
            await using var db = BuildContext();
            await SqlSafety.ProtectAsync(() => db.Database.OpenConnectionAsync(), SqlStage.ChildOpen);
            await SqlSchemaAssertions.VerifyAsync(db); // Refuse drift before any test DML.
            testReady = true;
        }
        catch
        {
            verified = false;
            testReady = false;
            if (lease != null) { await lease.DisposeAsync(); lease = null; }
            throw;
        }
    });

    // Future Phase B only. Explicit approval of the exact reviewed migration is required
    // in addition to the disposable-target gate. Never called by constructor or cleanup.
    public Task ApplyApprovedMigrationAsync() => SqlSafety.ProtectAsync(async () =>
    {
        RequireConfiguration("migrate");
        if (read(MigrationApproval) != Migration)
            throw new InvalidOperationException("SQL_MIGRATION_APPROVAL_REQUIRED");
        var commands = SqlSafety.ForwardCommands(); // Metadata only, before any connection.
        await OpenLeaseAsync("migrate");
        try
        {
            // Dispose the transaction before closing its connection. The outer finally also
            // releases the session lock if beginning the transaction itself fails.
            await using var transaction = (SqlTransaction)await lease!.BeginTransactionAsync();
            try
            {
                RequireConfiguration("migrate");
                if (read(MigrationApproval) != Migration) throw new InvalidOperationException("SQL_MIGRATION_APPROVAL_REQUIRED");
                foreach (var text in commands)
                {
                    await using var command = new SqlCommand(text, lease, transaction) { CommandTimeout = 30 };
                    await command.ExecuteNonQueryAsync();
                }
                await transaction.CommitAsync();
            }
            catch { await transaction.RollbackAsync(); throw; }
        }
        finally
        {
            verified = false;
            await lease!.DisposeAsync();
            lease = null;
        }
    });

    private async Task OpenLeaseAsync(string operation)
    {
        if (Interlocked.Exchange(ref sessionClaimed, 1) != 0) throw new InvalidOperationException("SQL_SESSION_ALREADY_USED");
        lease = new SqlConnection(connection);
        try
        {
            // Missing Initial Catalog fails here. No creator or master connection.
            await SqlSafety.ProtectAsync(() => lease.OpenAsync(), SqlStage.LeaseOpen);
            await SqlSafety.ProtectAsync(() => ValidateIdentityAsync(lease, operation), SqlStage.LeaseIdentity);
            await SqlSafety.ProtectAsync(async () =>
            {
                await using var command = new SqlCommand("DECLARE @result int; EXEC @result=sys.sp_getapplock @Resource=@resource, @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=0, @DbPrincipal='public'; SELECT @result;", lease);
                command.Parameters.AddWithValue("@resource", SqlSafety.LockResource);
                if (await command.ExecuteScalarAsync() is not int result || result is < 0 or > 1)
                    throw new InvalidOperationException("SQL_EXCLUSIVE_LOCK_UNAVAILABLE");
            }, SqlStage.LeaseLock);
            await SqlSafety.ProtectAsync(() => ValidateStateAsync(lease, operation), SqlStage.LeaseState);
            RequireConfiguration(operation);
            verified = true;
        }
        catch { await lease.DisposeAsync(); lease = null; throw; }
    }

    private async Task ValidateIdentityAsync(SqlConnection sql, string operation)
    {
        const string query = """
            SELECT CONVERT(nvarchar(128),SERVERPROPERTY('ComputerNamePhysicalNetBIOS')),
                   CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')), DB_NAME(),
                   CONVERT(int,SERVERPROPERTY('EngineEdition')),
                   CONVERT(int,CASE WHEN CONNECTIONPROPERTY('net_transport')='Shared memory' THEN 1 ELSE 0 END),
                   CONVERT(int,CASE WHEN SERVERPROPERTY('IsClustered')=0 AND SERVERPROPERTY('IsHadrEnabled')=0 AND d.group_database_id IS NULL THEN 1 ELSE 0 END),
                   CONVERT(int,CASE WHEN d.database_id>4 AND d.state=0 AND d.is_read_only=0 AND d.source_database_id IS NULL AND d.user_access=0 THEN 1 ELSE 0 END),
                   CONVERT(int,CASE WHEN SCHEMA_NAME()='dbo' THEN 1 ELSE 0 END),
                   CONVERT(int,CASE WHEN IS_SRVROLEMEMBER('sysadmin')=0 AND IS_SRVROLEMEMBER('dbcreator')=0
                       AND NOT EXISTS (SELECT 1 FROM sys.login_token WHERE type='SERVER ROLE' AND name<>'public')
                       AND IS_MEMBER('db_owner')=0 AND IS_MEMBER('db_securityadmin')=0 AND IS_MEMBER('db_ddladmin')=0
                       AND IS_MEMBER('db_accessadmin')=0 AND IS_MEMBER('db_backupoperator')=0
                       AND HAS_PERMS_BY_NAME(NULL,NULL,'CONTROL SERVER')=0 AND HAS_PERMS_BY_NAME(NULL,NULL,'CREATE ANY DATABASE')=0
                       AND HAS_PERMS_BY_NAME(NULL,NULL,'ALTER ANY DATABASE')=0 AND HAS_PERMS_BY_NAME(NULL,NULL,'ALTER ANY LOGIN')=0
                       AND HAS_PERMS_BY_NAME(NULL,NULL,'ALTER ANY SERVER ROLE')=0 AND HAS_PERMS_BY_NAME(NULL,NULL,'IMPERSONATE ANY LOGIN')=0
                       AND HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','CONTROL')=0 AND HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','ALTER')=0
                       AND HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','TAKE OWNERSHIP')=0
                       AND HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','ALTER ANY USER')=0 AND HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','ALTER ANY ROLE')=0 THEN 1 ELSE 0 END),
                   CONVERT(int,CASE WHEN HAS_PERMS_BY_NAME('dbo','SCHEMA','SELECT')=1 AND HAS_PERMS_BY_NAME('dbo','SCHEMA','INSERT')=1
                       AND HAS_PERMS_BY_NAME('dbo','SCHEMA','UPDATE')=1 AND HAS_PERMS_BY_NAME('dbo','SCHEMA','DELETE')=1 THEN 1 ELSE 0 END),
                   HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION'),
                   HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','CREATE TABLE'),
                   HAS_PERMS_BY_NAME('dbo','SCHEMA','ALTER'), HAS_PERMS_BY_NAME('dbo','SCHEMA','REFERENCES')
            FROM sys.databases d WHERE d.database_id=DB_ID();
            """;
        await using var command = new SqlCommand(query, sql);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync() || Enumerable.Range(0, 14).Any(reader.IsDBNull)) throw new InvalidOperationException("SQL_TARGET_EVIDENCE_INCOMPLETE");
        var e = new SqlSafety.Evidence(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3),
            reader.GetInt32(4) == 1, reader.GetInt32(5) == 1, reader.GetInt32(6) == 1, reader.GetInt32(7) == 1,
            reader.GetInt32(8) == 1, reader.GetInt32(9) == 1, reader.GetInt32(10) == 1,
            reader.GetInt32(11) == 1, reader.GetInt32(12) == 1, reader.GetInt32(13) == 1);
        SqlSafety.RequireEvidence(e, database, operation);
    }

    private static async Task ValidateStateAsync(SqlConnection sql, string operation)
    {
        var tables = new List<string>();
        await using (var command = new SqlCommand("SELECT SCHEMA_NAME(schema_id)+'.'+name FROM sys.tables WHERE is_ms_shipped=0 ORDER BY name", sql))
        await using (var reader = await command.ExecuteReaderAsync())
            while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        var history = new List<(string, string)>();
        if (tables.Contains("dbo.__EFMigrationsHistory"))
        {
            await using var command = new SqlCommand("SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId", sql);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) history.Add((reader.GetString(0), reader.GetString(1)));
        }
        await using var extra = new SqlCommand("SELECT (SELECT COUNT(*) FROM sys.objects WHERE is_ms_shipped=0 AND type NOT IN ('U','PK','F','C','D','UQ')) + (SELECT COUNT(*) FROM sys.types WHERE is_user_defined=1)", sql);
        var unexpected = Convert.ToInt32(await extra.ExecuteScalarAsync()) != 0 || tables.Any(x => !x.StartsWith("dbo.", StringComparison.Ordinal));
        SqlSafety.RequireState(operation, history, tables.Select(x => x.StartsWith("dbo.", StringComparison.Ordinal) ? x[4..] : "__UNEXPECTED_SCHEMA__").ToArray(), unexpected);
    }

    private async Task CheckSessionAsync(SqlConnection child)
    {
        RequireConfiguration("test");
        if (!verified || lease?.State != ConnectionState.Open) throw new InvalidOperationException("SQL_VERIFIED_SESSION_REQUIRED");
        await leaseGuard.WaitAsync();
        try
        {
            await SqlSafety.ProtectAsync(async () =>
            {
                await using var command = new SqlCommand("SELECT APPLOCK_MODE('public',@resource,'Session')", lease);
                command.Parameters.AddWithValue("@resource", SqlSafety.LockResource);
                if (!Equals(await command.ExecuteScalarAsync(), "Exclusive")) throw new InvalidOperationException("SQL_EXCLUSIVE_LOCK_LOST");
            }, SqlStage.SessionLock);
        }
        finally { leaseGuard.Release(); }
        await SqlSafety.ProtectAsync(() => ValidateIdentityAsync(child, "test"), SqlStage.ChildIdentity);
        await SqlSafety.ProtectAsync(() => ValidateStateAsync(child, "test"), SqlStage.ChildState);
    }

    private sealed class SessionGuard(SqlServerFixture fixture) : DbConnectionInterceptor
    {
        private void RequireSession()
        {
            fixture.RequireConfiguration("test");
            if (!fixture.verified || fixture.lease?.State != ConnectionState.Open) throw new InvalidOperationException("SQL_VERIFIED_SESSION_REQUIRED");
        }
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        { RequireSession(); return result; }
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        { RequireSession(); return ValueTask.FromResult(result); }
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData) =>
            SqlSafety.ProtectAsync(() => fixture.CheckSessionAsync((SqlConnection)connection)).GetAwaiter().GetResult();
        public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default) =>
            SqlSafety.ProtectAsync(() => fixture.CheckSessionAsync((SqlConnection)connection));
    }

    public async ValueTask DisposeAsync()
    {
        verified = false;
        testReady = false;
        if (lease != null) await lease.DisposeAsync(); // Closing releases session lock; no cleanup DDL/DML.
        leaseGuard.Dispose();
    }
}

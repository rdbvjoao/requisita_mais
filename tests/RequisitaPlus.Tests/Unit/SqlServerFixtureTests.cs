using Microsoft.EntityFrameworkCore;
using RequisitaPlus.Tests.Integration.SqlServer;
using RequisitaPlus.Tests.Support;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

public sealed class SqlServerFixtureTests
{
    private static Dictionary<string, string?> Settings() => new()
    {
        [IntegrationGate.SqlOptIn] = "true",
        [IntegrationGate.SqlConnection] = @"Server=.\REQUISITAPLUS;Database=RequisitaPlus_Test_Offline;Integrated Security=true",
        [IntegrationGate.DisposableDatabase] = "RequisitaPlus_Test_Offline",
        [IntegrationGate.MainDatabase] = "RequisitaPlus",
        [IntegrationGate.SqlOperation] = "test",
        [IntegrationGate.ExpectedMachine] = Environment.MachineName
    };

    [Theory]
    [InlineData(IntegrationGate.SqlOptIn, null)]
    [InlineData(IntegrationGate.SqlOptIn, "false")]
    [InlineData(IntegrationGate.SqlConnection, null)]
    [InlineData(IntegrationGate.DisposableDatabase, "RequisitaPlus")]
    [InlineData(IntegrationGate.MainDatabase, null)]
    [InlineData(IntegrationGate.MainDatabase, "RequisitaPlus_Test_Offline")]
    [InlineData(IntegrationGate.SqlConnection, "Server=(localdb)\\MSSQLLocalDB;Database=RequisitaPlus_Test_Offline;Integrated Security=true")]
    [InlineData(IntegrationGate.SqlConnection, "Server=sql-tests.invalid;Database=RequisitaPlus;Integrated Security=true")]
    [InlineData(IntegrationGate.SqlConnection, "invalid")]
    [InlineData(IntegrationGate.SqlConnection, "Server=sql-tests.invalid;Database=RequisitaPlus_Test_Offline;Failover Partner=other.invalid;Integrated Security=true")]
    [InlineData(IntegrationGate.SqlConnection, "Server=sql-tests.invalid;Database=RequisitaPlus_Test_Offline;Application Intent=ReadOnly;Integrated Security=true")]
    public void UnsafeConfigurationIsRejectedBeforeAnyIo(string key, string? value)
    {
        var settings = Settings();
        settings[key] = value;
        Assert.Throws<InvalidOperationException>(() => new SqlServerFixture(settings.GetValueOrDefault));
    }

    [Fact]
    public async Task ContextRequiresVerifiedSessionAndMigrationRequiresSeparateApproval()
    {
        var settings = Settings();
        await using var fixture = new SqlServerFixture(settings.GetValueOrDefault);
        Assert.Throws<InvalidOperationException>(fixture.CreateContext);
        await Assert.ThrowsAsync<InvalidOperationException>(fixture.ApplyApprovedMigrationAsync);
        settings[IntegrationGate.SqlOperation] = "migrate";
        await Assert.ThrowsAsync<InvalidOperationException>(fixture.ApplyApprovedMigrationAsync);
        settings[IntegrationGate.SqlOptIn] = "false";
        Assert.Throws<InvalidOperationException>(() => fixture.CreateContext());
    }

    [Theory]
    [InlineData(@".\REQUISITAPLUS", true)]
    [InlineData(@".\requisitaplus", true)]
    [InlineData(@"lpc:.\REQUISITAPLUS", true)]
    [InlineData(@"localhost\REQUISITAPLUS", false)]
    [InlineData(@"127.0.0.1\REQUISITAPLUS", false)]
    [InlineData(@"tcp:.\REQUISITAPLUS", false)]
    [InlineData(@"remote\REQUISITAPLUS", false)]
    [InlineData(@".\SQLEXPRESS", false)]
    [InlineData(@".\REQUISITAPLUS,1433", false)]
    public void OnlyLiteralLocalInstanceAndSharedMemoryAreAllowed(string server, bool expected)
    {
        var settings = Settings();
        settings[IntegrationGate.SqlConnection] = $"Server={server};Database=RequisitaPlus_Test_Offline;Integrated Security=true;HostNameInCertificate={Environment.MachineName}";
        Assert.Equal(expected, IntegrationGate.IsSqlEnabled(settings.GetValueOrDefault));
    }

    [Theory]
    [InlineData("Failover Partner=remote")]
    [InlineData("Application Intent=ReadOnly")]
    [InlineData("MultiSubnetFailover=true")]
    [InlineData("User Id=secret;Password=secret")]
    [InlineData("TrustServerCertificate=true")]
    [InlineData("Encrypt=false")]
    [InlineData("Connect Timeout=0")]
    [InlineData("HostNameInCertificate=remote")]
    [InlineData("AttachDbFilename=test.mdf")]
    [InlineData("ConnectRetryCount=3")]
    [InlineData("ServerCertificate=certificate.cer")]
    public void AlternativeTransportCredentialsAndUnsafeOptionsFailClosed(string options)
    {
        var settings = Settings();
        settings[IntegrationGate.SqlConnection] += ";" + options;
        Assert.False(IntegrationGate.IsSqlEnabled(settings.GetValueOrDefault));
    }

    [Fact]
    public async Task CertificateNameChangeIsRejectedBeforeOpeningSession()
    {
        var settings = Settings();
        settings[IntegrationGate.SqlConnection] += $";HostNameInCertificate={Environment.MachineName}";
        await using var fixture = new SqlServerFixture(settings.GetValueOrDefault);
        settings[IntegrationGate.SqlConnection] += ";HostNameInCertificate=remote.invalid";
        await Assert.ThrowsAsync<InvalidOperationException>(fixture.OpenForTestsAsync);
    }

    [Fact]
    public void ConfigurationIsSnapshottedWithoutReturningASecondUnvalidatedRead()
    {
        var settings = Settings();
        var reads = 0;
        string? Read(string name) => name == IntegrationGate.SqlConnection && ++reads > 1 ? "Server=remote;Database=Principal" : settings.GetValueOrDefault(name);
        Assert.Contains("lpc:", IntegrationGate.RequireSqlConnectionString(Read));
        Assert.Equal(1, reads);
    }

    private static SqlSafety.Evidence ValidEvidence() => new(Environment.MachineName, "REQUISITAPLUS", "RequisitaPlus_Test_Offline", 4,
        true, true, true, true, true, true, true, false, false, false);

    [Fact]
    public void EffectiveIdentityAndPermissionsRequireRealEvidenceBeyondConnectionText()
    {
        var valid = ValidEvidence();
        SqlSafety.RequireEvidence(valid, valid.Database, "test");
        foreach (var invalid in new[] { valid with { Machine = "remote" }, valid with { Instance = "other" }, valid with { Database = "Principal" },
            valid with { EngineEdition = 3 }, valid with { LocalTransport = false }, valid with { Standalone = false }, valid with { Writable = false },
            valid with { DefaultDbo = false }, valid with { Restricted = false }, valid with { CanReadWrite = false }, valid with { CanInspect = false },
            valid with { CanCreateTable = true }, valid with { CanAlterDbo = true } })
            Assert.Throws<InvalidOperationException>(() => SqlSafety.RequireEvidence(invalid, valid.Database, "test"));
        Assert.Throws<InvalidOperationException>(() => SqlSafety.RequireEvidence(valid, valid.Database, "migrate"));
        SqlSafety.RequireEvidence(valid with { CanCreateTable = true, CanAlterDbo = true, CanReference = true }, valid.Database, "migrate");
    }

    [Fact]
    public void EmptyDatabaseAndAppliedSchemaAreDistinctStatesAndUnknownHistoryIsRejected()
    {
        SqlSafety.RequireState("migrate", [], [], false);
        SqlSafety.RequireState("test", [(SqlServerFixture.Migration, SqlSafety.Version)], SqlSafety.Tables, false);
        Assert.Throws<InvalidOperationException>(() => SqlSafety.RequireState("migrate", [], ["Existing"], false));
        Assert.Throws<InvalidOperationException>(() => SqlSafety.RequireState("migrate", [(SqlServerFixture.Migration, SqlSafety.Version)], SqlSafety.Tables, false));
        Assert.Throws<InvalidOperationException>(() => SqlSafety.RequireState("test", [], [], false));
        Assert.Throws<InvalidOperationException>(() => SqlSafety.RequireState("test", [("future", SqlSafety.Version)], SqlSafety.Tables, false));
        Assert.Throws<InvalidOperationException>(() => SqlSafety.RequireState("test", [(SqlServerFixture.Migration, "other")], SqlSafety.Tables, false));
        Assert.Throws<InvalidOperationException>(() => SqlSafety.RequireState("test", [(SqlServerFixture.Migration, SqlSafety.Version), ("future", SqlSafety.Version)], SqlSafety.Tables, false));
        Assert.Throws<InvalidOperationException>(() => SqlSafety.RequireState("test", [(SqlServerFixture.Migration, SqlSafety.Version)], SqlSafety.Tables, true));
        Assert.Throws<InvalidOperationException>(() => SqlSafety.RequireState("test", [(SqlServerFixture.Migration, SqlSafety.Version)], SqlSafety.Tables.Skip(1).ToArray(), false));
    }

    [Fact]
    public void ApprovedForwardCommandsAreOfflineTransactionalAndCannotCreateOrDropDatabases()
    {
        var sql = string.Join('\n', SqlSafety.ForwardCommands());
        Assert.Contains("CREATE TABLE", sql);
        Assert.Contains(SqlServerFixture.Migration, sql);
        Assert.DoesNotContain("CREATE DATABASE", sql);
        Assert.DoesNotContain("DROP", sql);
        Assert.DoesNotContain("ALTER DATABASE", sql);
        Assert.DoesNotContain("USE ", sql);
    }

    [Fact]
    public void SqlFailuresExposeOnlyCodesAndApprovedObjectNames()
    {
        var error = SqlSafety.Redact(547, "SQL secret-password secret-hash Server=private;Database=private; 'CK_Requisito_Status' 'CK_private_data'");
        Assert.Equal(547, error.Number);
        Assert.Contains("CK_Requisito_Status", error.Objects);
        Assert.DoesNotContain("private", error.ToString());
        Assert.DoesNotContain("secret", error.ToString());
        Assert.Null(error.InnerException);
    }
}

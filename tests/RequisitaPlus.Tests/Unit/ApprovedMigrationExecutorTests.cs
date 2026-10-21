using RequisitaPlus.Tests.Integration.SqlServer;
using RequisitaPlus.Tests.Operations;
using RequisitaPlus.Tests.Support;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

public sealed class ApprovedMigrationExecutorTests
{
    private static string[] ApprovedArgs() => ["--apply-approved-migration", SqlServerFixture.Migration];

    // Entirely synthetic; not the name of any confirmed principal database.
    private static Dictionary<string, string?> Settings() => new()
    {
        [IntegrationGate.SqlOptIn] = "true",
        [IntegrationGate.SqlConnection] = @"Server=.\REQUISITAPLUS;Database=RequisitaPlus_Test_Offline;Integrated Security=true",
        [IntegrationGate.DisposableDatabase] = "RequisitaPlus_Test_Offline",
        [IntegrationGate.MainDatabase] = "Offline_Main_Sentinel",
        [IntegrationGate.SqlOperation] = "migrate",
        [IntegrationGate.ExpectedMachine] = Environment.MachineName,
        [SqlServerFixture.MigrationApproval] = SqlServerFixture.Migration
    };

    [Theory]
    [InlineData(IntegrationGate.SqlOptIn, null)]
    [InlineData(IntegrationGate.SqlOptIn, "false")]
    [InlineData(IntegrationGate.SqlOperation, "test")]
    [InlineData(IntegrationGate.SqlOperation, null)]
    [InlineData(SqlServerFixture.MigrationApproval, null)]
    [InlineData(SqlServerFixture.MigrationApproval, "other")]
    [InlineData(SqlServerFixture.MigrationApproval, "20261019220059_initialcreate")]
    [InlineData(IntegrationGate.MainDatabase, null)]
    [InlineData(IntegrationGate.SqlConnection, "Server=remote;Database=private;Password=secret")]
    public async Task MissingUnsafeOrUnapprovedConfigurationNeverConstructsSession(string key, string? value)
    {
        var settings = Settings();
        settings[key] = value;
        var created = 0;
        var executor = new ApprovedMigrationExecutor(settings.GetValueOrDefault, () =>
        {
            created++;
            throw new InvalidOperationException("Session must never be constructed.");
        });
        var result = await executor.RunAsync(ApprovedArgs());
        Assert.Equal(2, result.ExitCode);
        Assert.Equal(0, created);
        Assert.DoesNotContain("secret", result.Diagnostic);
        Assert.DoesNotContain("private", result.Diagnostic);
    }

    public static TheoryData<string[]> RejectedArguments => new()
    {
        Array.Empty<string>(),
        new[] { "--apply-approved-migration" },
        new[] { "--apply-approved-migration", "other" },
        new[] { "--apply-approved-migration", "20261019220059_initialcreate" },
        new[] { "--sql", "SELECT 1" },
        new[] { "--apply-approved-migration", "DROP DATABASE private" },
        new[] { "--apply-approved-migration", SqlServerFixture.Migration, "SELECT 1" },
        new[] { "--apply-approved-migration", SqlServerFixture.Migration, "--force" },
        new[] { "--down", SqlServerFixture.Migration },
        new[] { "--test", SqlServerFixture.Migration }
    };

    [Theory]
    [MemberData(nameof(RejectedArguments))]
    public async Task OnlyExactClosedArgumentListIsAccepted(string[] args)
    {
        var session = new FakeSession();
        var result = await new ApprovedMigrationExecutor(Settings().GetValueOrDefault, () => session).RunAsync(args);
        Assert.Equal(2, result.ExitCode);
        Assert.Equal(0, session.Applications);
        Assert.Equal(0, session.Disposals);
        Assert.DoesNotContain("SELECT", result.Diagnostic);
        Assert.DoesNotContain("DROP", result.Diagnostic);
    }

    [Fact]
    public async Task ApprovedAttemptInvokesOnlyOneApplicationAndDisposesSession()
    {
        var session = new FakeSession();
        var executor = new ApprovedMigrationExecutor(Settings().GetValueOrDefault, () => session);
        Assert.Equal(0, (await executor.RunAsync(ApprovedArgs())).ExitCode);
        Assert.Equal(2, (await executor.RunAsync(ApprovedArgs())).ExitCode);
        Assert.Equal(1, session.Applications);
        Assert.Equal(1, session.Disposals);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CertificateNameIsValidatedBeforeConstructingExecutorSession(bool local)
    {
        var settings = Settings();
        settings[IntegrationGate.SqlConnection] += $";HostNameInCertificate={(local ? Environment.MachineName : "remote.invalid")}";
        var session = new FakeSession();
        var created = 0;
        var executor = new ApprovedMigrationExecutor(settings.GetValueOrDefault, () => { created++; return session; });
        var result = await executor.RunAsync(ApprovedArgs());
        Assert.Equal(local ? 0 : 2, result.ExitCode);
        Assert.Equal(local ? 1 : 0, created);
        Assert.Equal(local ? 1 : 0, session.Applications);
        Assert.Equal(local ? 1 : 0, session.Disposals);
    }

    [Theory]
    [InlineData("construct")]
    [InlineData("apply")]
    [InlineData("dispose")]
    public async Task FailuresAreSanitizedAndNeverRetriedOrSentToFallback(string failure)
    {
        var session = new FakeSession { FailApply = failure == "apply", FailDispose = failure == "dispose" };
        var created = 0;
        var executor = new ApprovedMigrationExecutor(Settings().GetValueOrDefault, () =>
        {
            created++;
            if (failure == "construct") throw new InvalidOperationException("Password=private-secret");
            return session;
        });
        var result = await executor.RunAsync(ApprovedArgs());
        Assert.Equal(new MigrationExecutionResult(1, "SQL_MIGRATION_EXECUTOR_FAILED_REVIEW_REQUIRED"), result);
        Assert.Equal(2, (await executor.RunAsync(ApprovedArgs())).ExitCode);
        Assert.Equal(1, created);
        Assert.Equal(failure == "construct" ? 0 : 1, session.Applications);
        Assert.Equal(failure == "construct" ? 0 : 1, session.Disposals);
        Assert.DoesNotContain("private", result.Diagnostic);
    }

    [Fact]
    public async Task ConcurrentCallsShareOneAttempt()
    {
        var session = new FakeSession();
        var executor = new ApprovedMigrationExecutor(Settings().GetValueOrDefault, () => session);
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => executor.RunAsync(ApprovedArgs()))));
        Assert.Single(results, result => result.ExitCode == 0);
        Assert.Equal(1, session.Applications);
        Assert.Equal(1, session.Disposals);
    }

    [Fact]
    public void SysadminEvidenceRemainsRejectedEvenWithAllMigrationPermissions()
    {
        // Restricted=false represents rejection by the existing SQL identity query,
        // including IS_SRVROLEMEMBER('sysadmin'). No server identity is queried here.
        var evidence = new SqlSafety.Evidence(Environment.MachineName, "REQUISITAPLUS", "RequisitaPlus_Test_Offline", 4,
            true, true, true, true, false, true, true, true, true, true);
        Assert.Throws<InvalidOperationException>(() => SqlSafety.RequireEvidence(evidence, evidence.Database, "migrate"));
    }

    [Fact]
    public async Task RealFixtureRejectsTestModeOrWrongApprovalBeforeConnection()
    {
        var settings = Settings();
        settings[IntegrationGate.SqlOperation] = "test";
        await using (var fixture = new SqlServerFixture(settings.GetValueOrDefault))
            await Assert.ThrowsAsync<InvalidOperationException>(fixture.ApplyApprovedMigrationAsync);
        settings[IntegrationGate.SqlOperation] = "migrate";
        settings[SqlServerFixture.MigrationApproval] = "other";
        await using (var fixture = new SqlServerFixture(settings.GetValueOrDefault))
            await Assert.ThrowsAsync<InvalidOperationException>(fixture.ApplyApprovedMigrationAsync);
    }

    private sealed class FakeSession : IApprovedMigrationSession
    {
        public int Applications { get; private set; }
        public int Disposals { get; private set; }
        public bool FailApply { get; init; }
        public bool FailDispose { get; init; }

        public Task ApplyApprovedMigrationAsync()
        {
            Applications++;
            return FailApply ? Task.FromException(new InvalidOperationException("Password=private-secret")) : Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Disposals++;
            return FailDispose ? ValueTask.FromException(new InvalidOperationException("Server=private-secret")) : ValueTask.CompletedTask;
        }
    }
}

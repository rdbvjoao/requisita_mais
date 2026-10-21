using RequisitaPlus.Tests.Integration.SqlServer;
using RequisitaPlus.Tests.Support;

namespace RequisitaPlus.Tests.Operations;

// The real entry point has no injectable action or alternate execution path.
internal static class MigrationExecutorProgram
{
    public static async Task<int> Main(string[] args)
    {
        var result = await new ApprovedMigrationExecutor().RunAsync(args);
        Console.Out.WriteLine(result.Diagnostic);
        return result.ExitCode;
    }
}

internal sealed record MigrationExecutionResult(int ExitCode, string Diagnostic);

// Internal seam for offline tests. There is deliberately no SQL/command parameter.
internal interface IApprovedMigrationSession : IAsyncDisposable
{
    Task ApplyApprovedMigrationAsync();
}

internal sealed class ApprovedMigrationExecutor
{
    private readonly Func<string, string?> read;
    private readonly Func<IApprovedMigrationSession> createSession;
    private int claimed;

    public ApprovedMigrationExecutor() : this(Environment.GetEnvironmentVariable) { }

    private ApprovedMigrationExecutor(Func<string, string?> read)
        : this(read, () => new FixtureSession(read)) { }

    internal ApprovedMigrationExecutor(Func<string, string?> read, Func<IApprovedMigrationSession> createSession)
    {
        this.read = read;
        this.createSession = createSession;
    }

    public async Task<MigrationExecutionResult> RunAsync(string[] args)
    {
        // Consume even a rejected attempt. Another process still must pass all SQL guards.
        if (Interlocked.Exchange(ref claimed, 1) != 0)
            return new(2, "SQL_MIGRATION_EXECUTOR_ALREADY_USED");

        try
        {
            if (args.Length != 2 || args[0] != "--apply-approved-migration" || args[1] != SqlServerFixture.Migration
                || read(IntegrationGate.SqlOperation) != "migrate"
                || read(SqlServerFixture.MigrationApproval) != SqlServerFixture.Migration)
                return new(2, "SQL_MIGRATION_EXECUTOR_AUTHORIZATION_REJECTED");

            _ = IntegrationGate.RequireSqlConnectionString(read); // Parsing only, before constructing a session.
        }
        catch (Exception)
        {
            return new(2, "SQL_MIGRATION_EXECUTOR_CONFIGURATION_REJECTED");
        }

        try
        {
            await using var session = createSession();
            await session.ApplyApprovedMigrationAsync();
            return new(0, "SQL_MIGRATION_EXECUTOR_COMPLETED");
        }
        catch (Exception)
        {
            // Also covers constructor/transaction/rollback/disposal errors. Never print exception text,
            // stack traces, connection strings or claim rollback/commit certainty after a failure.
            return new(1, "SQL_MIGRATION_EXECUTOR_FAILED_REVIEW_REQUIRED");
        }
    }

    private sealed class FixtureSession(Func<string, string?> read) : IApprovedMigrationSession
    {
        private readonly SqlServerFixture fixture = new(read);
        public Task ApplyApprovedMigrationAsync() => fixture.ApplyApprovedMigrationAsync();
        public ValueTask DisposeAsync() => fixture.DisposeAsync();
    }
}

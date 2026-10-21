using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Metadata;
using RequisitaPlus.Web.Data;

namespace RequisitaPlus.Tests.Integration.SqlServer;

/// <summary>Offline policy and sanitized diagnostics; never retains SQL exception messages.</summary>
public static class SqlSafety
{
    public const string LockResource = "RequisitaPlus.SqlExclusive.v1";
    public const string Version = "10.0.12";
    public static readonly string[] Tables = ["AspNetRoles", "AspNetUsers", "AspNetRoleClaims", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserRoles", "AspNetUserTokens", "Requisito", "OperacaoAnalise", "Analise", "Sugestao", "__EFMigrationsHistory"];

    public sealed record Evidence(string Machine, string Instance, string Database, int EngineEdition,
        bool LocalTransport, bool Standalone, bool Writable, bool DefaultDbo, bool Restricted,
        bool CanReadWrite, bool CanInspect, bool CanCreateTable, bool CanAlterDbo, bool CanReference);

    public static void RequireEvidence(Evidence e, string database, string operation)
    {
        if (!e.Machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
            || !e.Instance.Equals("REQUISITAPLUS", StringComparison.OrdinalIgnoreCase)
            || !e.Database.Equals(database, StringComparison.Ordinal) || e.EngineEdition != 4
            || !e.LocalTransport || !e.Standalone || !e.Writable || !e.DefaultDbo
            || !e.Restricted || !e.CanReadWrite || !e.CanInspect
            || (operation == "migrate" ? !(e.CanCreateTable && e.CanAlterDbo && e.CanReference)
                : operation != "test" || e.CanCreateTable || e.CanAlterDbo || e.CanReference))
            throw new InvalidOperationException("SQL_TARGET_EVIDENCE_REJECTED");
    }

    public static void RequireState(string operation, IReadOnlyList<(string Id, string Version)> history,
        IReadOnlyList<string> tables, bool unexpectedObjects)
    {
        if (operation == "migrate")
        {
            if (history.Count != 0 || tables.Count != 0 || unexpectedObjects)
                throw new InvalidOperationException("SQL_MIGRATION_REQUIRES_PRISTINE_EXISTING_DATABASE");
        }
        else if (operation != "test" || history.Count != 1 || history[0] != (SqlServerFixture.Migration, Version)
            || !tables.Order().SequenceEqual(Tables.Order()) || unexpectedObjects)
            throw new InvalidOperationException("SQL_SCHEMA_HISTORY_INCOMPATIBLE");
    }

    public static IReadOnlyList<string> ForwardCommands()
    {
        // Render only the existing approved migration, without a migrator or creator.
        using var db = new ApplicationDbContextFactory().CreateDbContext([]);
        var assembly = db.GetService<IMigrationsAssembly>();
        if (assembly.Migrations.Count != 1 || !assembly.Migrations.TryGetValue(SqlServerFixture.Migration, out var type)
            || db.Database.HasPendingModelChanges()) throw new InvalidOperationException("SQL_MIGRATION_ARTIFACT_MISMATCH");
        var migration = assembly.CreateMigration(type, db.Database.ProviderName!);
        if (migration.UpOperations.Any(x => x is not (CreateTableOperation or CreateIndexOperation)))
            throw new InvalidOperationException("SQL_MIGRATION_OPERATION_NOT_APPROVED");
        var generated = db.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations, db.GetService<IDesignTimeModel>().Model);
        if (generated.Any(x => x.TransactionSuppressed)) throw new InvalidOperationException("SQL_NONTRANSACTIONAL_DDL_REJECTED");
        var history = db.GetService<IHistoryRepository>();
        return new[] { history.GetCreateScript() }.Concat(generated.Select(x => x.CommandText))
            .Append(history.GetInsertScript(new HistoryRow(SqlServerFixture.Migration, Version))).ToArray();
    }

    private static readonly Lazy<HashSet<string>> DiagnosticNames = new(() =>
    {
        using var db = new ApplicationDbContextFactory().CreateDbContext([]);
        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(assembly.Migrations[SqlServerFixture.Migration], db.Database.ProviderName!);
        return migration.UpOperations.OfType<CreateTableOperation>().SelectMany(t =>
            t.CheckConstraints.Select(c => c.Name).Concat(t.ForeignKeys.Select(f => f.Name))
                .Concat(t.Columns.Select(c => c.Name)).Append(t.PrimaryKey!.Name))
            .Concat(migration.UpOperations.OfType<CreateIndexOperation>().Select(i => i.Name)).ToHashSet(StringComparer.Ordinal);
    });

    public static SqlTestFailure Redact(int number, string untrustedMessage) => new(number,
        DiagnosticNames.Value.Where(name => untrustedMessage.Contains("'" + name + "'", StringComparison.Ordinal)
            || untrustedMessage.Contains("\"" + name + "\"", StringComparison.Ordinal)).ToArray());

    public static async Task ProtectAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception exception) when (FindSql(exception) != null)
        {
            var sql = FindSql(exception)!;
            throw Redact(sql.Number, sql.Message);
        }
        catch (DbUpdateException exception) when (exception is not DbUpdateConcurrencyException)
        {
            if (exception.InnerException is SqlException sql) throw Redact(sql.Number, sql.Message);
            throw new SqlTestFailure(-1, []);
        }
    }

    // Fixed identifiers only. Preserve the innermost failure across nested guards;
    // never retain the original exception or any connection/query payload.
    public static async Task ProtectAsync(Func<Task> action, SqlStage stage)
    {
        if (!Enum.IsDefined(stage)) throw new ArgumentOutOfRangeException(nameof(stage));
        try { await ProtectAsync(action); }
        catch (SqlTestFailure failure) when (failure.Stage == null)
        {
            throw new SqlTestFailure(failure.Number, [], stage);
        }
    }

    private static SqlException? FindSql(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current is SqlException sql) return sql;
            if (current is AggregateException aggregate)
                foreach (var inner in aggregate.InnerExceptions)
                    if (FindSql(inner) is SqlException nested) return nested;
        }
        return null;
    }
}

public enum SqlStage
{
    LeaseOpen, LeaseIdentity, LeaseLock, LeaseState,
    ChildOpen, SessionLock, ChildIdentity, ChildState, CatalogQuery
}

public sealed class SqlTestFailure(int number, IReadOnlyList<string> objects, SqlStage? stage = null)
    : Exception(stage is null
        ? $"SQL_FAILURE code={number}; objects={string.Join(',', objects)}"
        : $"SQL_FAILURE code={number}; stage={stage}")
{
    public int Number { get; } = number;
    public IReadOnlyList<string> Objects { get; } = objects;
    public SqlStage? Stage { get; } = stage;
}

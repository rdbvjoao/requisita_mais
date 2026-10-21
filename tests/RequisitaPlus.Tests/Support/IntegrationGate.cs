using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace RequisitaPlus.Tests.Support;

/// <summary>
/// Technical opt-in only; it never grants human authorization or contacts a service.
/// Future fixtures must call RequireSqlConnectionString/RequireOllama before creating
/// resources, including when invoked outside an attributed test. Never read appsettings
/// or DefaultConnection as a fallback. Do not log the connection string.
/// </summary>
public static class IntegrationGate
{
    public const string SqlOptIn = "REQUISITAPLUS_TEST_SQL_ENABLED";
    public const string OllamaOptIn = "REQUISITAPLUS_TEST_OLLAMA_ENABLED";
    public const string SqlConnection = "REQUISITAPLUS_TEST_SQL_CONNECTION";
    public const string DisposableDatabase = "REQUISITAPLUS_TEST_SQL_DATABASE";
    public const string MainDatabase = "REQUISITAPLUS_MAIN_SQL_DATABASE";
    public const string SqlOperation = "REQUISITAPLUS_TEST_SQL_OPERATION";
    public const string ExpectedMachine = "REQUISITAPLUS_TEST_SQL_EXPECTED_MACHINE";
    public const string LocalInstance = @".\REQUISITAPLUS";

    public static bool IsSqlEnabled() => IsSqlEnabled(Environment.GetEnvironmentVariable);
    public static bool IsOllamaEnabled() => IsOllamaEnabled(Environment.GetEnvironmentVariable);

    // Injectable readers let local tests exercise policy without changing process state.
    public static bool IsOllamaEnabled(Func<string, string?> read) => read(OllamaOptIn) == "true";

    public static bool IsSqlEnabled(Func<string, string?> read) => TrySqlSettings(read, out _);

    public static bool IsSqlTestEnabled() => IsSqlEnabled() && Environment.GetEnvironmentVariable(SqlOperation) == "test";

    private static bool TrySqlSettings(Func<string, string?> read, out string? canonical)
    {
        canonical = null;
        // Snapshot once: never return an unvalidated second read of the secret.
        var values = new[] { SqlOptIn, DisposableDatabase, MainDatabase, SqlConnection, SqlOperation, ExpectedMachine }
            .ToDictionary(key => key, read);
        if (values[SqlOptIn] != "true" || values[SqlOperation] is not ("test" or "migrate")
            || !string.Equals(values[ExpectedMachine], Environment.MachineName, StringComparison.OrdinalIgnoreCase)) return false;
        var database = values[DisposableDatabase];
        var main = values[MainDatabase];
        var connection = values[SqlConnection];
        if (database is null || !Regex.IsMatch(database, @"\ARequisitaPlus_Test_[A-Za-z0-9_]{1,100}\z")
            || string.IsNullOrWhiteSpace(main) || main != main.Trim()
            || string.Equals(database, main, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(connection))
            return false;

        try
        {
            // Parsing only: no SqlConnection is constructed or opened.
            var builder = new SqlConnectionStringBuilder(connection);
            // Only the literal local instance (case-insensitive) and its explicit
            // shared-memory spelling. DNS names, machine aliases and ports are not evidence.
            if (!builder.DataSource.Equals(LocalInstance, StringComparison.OrdinalIgnoreCase)
                && !builder.DataSource.Equals("lpc:" + LocalInstance, StringComparison.OrdinalIgnoreCase)) return false;
            var allowed = new[] { "datasource", "initialcatalog", "integratedsecurity", "encrypt", "trustservercertificate", "hostnameincertificate", "connecttimeout", "applicationname" };
            if (builder.Keys.Cast<string>().Where(builder.ShouldSerialize)
                .Any(key => !allowed.Contains(key.Replace(" ", "").ToLowerInvariant()))) return false;
            if (!builder.InitialCatalog.Equals(database, StringComparison.Ordinal) || !builder.IntegratedSecurity
                || builder.TrustServerCertificate || builder.Encrypt != SqlConnectionEncryptOption.Mandatory
                || builder.ConnectTimeout is < 1 or > 15) return false;
            // A TLS name override never changes the endpoint or proves SQL identity.
            // Accept only this machine's name, without aliases, suffixes or whitespace.
            if (builder.ShouldSerialize("HostNameInCertificate")
                && !builder.HostNameInCertificate.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)) return false;
            builder.HostNameInCertificate = Environment.MachineName;
            // Shared memory cannot route to a remote endpoint or an availability listener.
            // Still not proof of server identity: the future session verifies SQL evidence.
            builder.DataSource = "lpc:" + LocalInstance;
            builder.Pooling = false;
            builder.ConnectRetryCount = 0;
            builder.Enlist = false;
            builder.MultipleActiveResultSets = false;
            builder.PersistSecurityInfo = false;
            builder.ApplicationName = "RequisitaPlus.SqlSafety";
            canonical = builder.ConnectionString;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static string RequireSqlConnectionString() => RequireSqlConnectionString(Environment.GetEnvironmentVariable);

    public static string RequireSqlConnectionString(Func<string, string?> read)
    {
        if (!TrySqlSettings(read, out var canonical))
            throw new InvalidOperationException("SQL integration disabled: explicit disposable test database configuration required.");

        return canonical!;
    }

    public static void RequireOllama()
    {
        if (!IsOllamaEnabled())
            throw new InvalidOperationException("Ollama integration disabled: explicit opt-in required.");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RequisitaPlus.Tests.Integration.SqlServer;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.Services;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

public sealed class SqlSafetyDiagnosticsTests
{
    [Theory]
    [InlineData(SqlStage.LeaseOpen)]
    [InlineData(SqlStage.LeaseIdentity)]
    [InlineData(SqlStage.LeaseLock)]
    [InlineData(SqlStage.LeaseState)]
    [InlineData(SqlStage.ChildOpen)]
    [InlineData(SqlStage.SessionLock)]
    [InlineData(SqlStage.ChildIdentity)]
    [InlineData(SqlStage.ChildState)]
    [InlineData(SqlStage.CatalogQuery)]
    public async Task TimeoutStageContainsOnlyFixedIdentifierAndCode(SqlStage stage)
    {
        var failure = await Assert.ThrowsAsync<SqlTestFailure>(() => SqlSafety.ProtectAsync(() =>
            Task.FromException(SqlSafety.Redact(-2, "Server=private;secret 'CK_Requisito_Status'")), stage));
        Assert.Equal(-2, failure.Number);
        Assert.Equal(stage, failure.Stage);
        Assert.Equal($"SQL_FAILURE code=-2; stage={stage}", failure.Message);
        Assert.Empty(failure.Objects);
        Assert.Null(failure.InnerException);
        Assert.DoesNotContain("private", failure.ToString());
        Assert.DoesNotContain("secret", failure.ToString());
    }

    [Fact]
    public async Task NestedGuardsPreserveInnermostStage()
    {
        var failure = await Assert.ThrowsAsync<SqlTestFailure>(() => SqlSafety.ProtectAsync(() =>
            SqlSafety.ProtectAsync(() => SqlSafety.ProtectAsync(() =>
                Task.FromException(SqlSafety.Redact(-2, "private")), SqlStage.ChildIdentity), SqlStage.ChildOpen)));
        Assert.Equal(SqlStage.ChildIdentity, failure.Stage);
        Assert.Null(failure.InnerException);
    }

    [Fact]
    public async Task StageDoesNotConvertOrSuppressPolicyRejection()
    {
        var rejection = new InvalidOperationException("SQL_TARGET_EVIDENCE_REJECTED");
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => SqlSafety.ProtectAsync(() =>
            Task.FromException(rejection), SqlStage.LeaseIdentity));
        Assert.Same(rejection, actual);
    }

    [Fact]
    public async Task ConcurrentStagesDoNotShareMutableDiagnosticState()
    {
        async Task<SqlTestFailure> Fail(SqlStage stage) => await Assert.ThrowsAsync<SqlTestFailure>(() =>
            SqlSafety.ProtectAsync(async () =>
            {
                await Task.Yield();
                throw SqlSafety.Redact(-2, "private");
            }, stage));
        var failures = await Task.WhenAll(Fail(SqlStage.ChildIdentity), Fail(SqlStage.SessionLock));
        Assert.Equal(SqlStage.ChildIdentity, failures[0].Stage);
        Assert.Equal(SqlStage.SessionLock, failures[1].Stage);
    }

    [Theory]
    [InlineData("[A]>0", "(([a] > (0)))")]
    [InlineData("[A] IS NULL OR [A]>=0", "(([a] IS NULL) OR ([a]>=(0)))")]
    [InlineData("[Decisao] IN (N'Aplicada', N'Ajustada')", "([Decisao] IN (N'Aplicada',N'Ajustada'))")]
    [InlineData("DATALENGTH([Nome])<=200", "(datalength([Nome])<=(200))")]
    [InlineData("[OrdemNoResultado] BETWEEN 1 AND 5", "([OrdemNoResultado]>=(1) AND [OrdemNoResultado]<=(5))")]
    [InlineData("[Decisao] IN (N'Aplicada', N'Ajustada')", "([Decisao]=N'Ajustada' OR [Decisao]=N'Aplicada')")]
    [InlineData("([A]=1 OR [B]=2) OR [C]=3", "[C]=3 OR ([B]=2 OR [A]=1)")]
    public void CanonicalizationIgnoresOnlyProvenFormatting(string expected, string actual)
    {
        Assert.Equal(CheckExpression.Canonical(expected), CheckExpression.Canonical(actual));
    }

    [Theory]
    [InlineData("[A]=1 AND ([B]=2 OR [C]=3)", "([A]=1 AND [B]=2) OR [C]=3")]
    [InlineData("[A]=N'Pendente'", "[A]=N'pendente'")]
    [InlineData("[A]<=5000", "[A]<=5001")]
    [InlineData("[A] COLLATE Latin1_General_100_BIN2=N'Alta'", "[A]=N'Alta'")]
    public void CanonicalizationPreservesPrecedenceLiteralCaseLimitsAndCollation(string expected, string actual) =>
        Assert.NotEqual(CheckExpression.Canonical(expected), CheckExpression.Canonical(actual));

    [Fact]
    public void EveryApprovedCheckAndFilterCanBeParsedOffline()
    {
        using var db = new ApplicationDbContextFactory().CreateDbContext([]);
        foreach (var entity in db.GetService<IDesignTimeModel>().Model.GetEntityTypes())
        {
            foreach (var check in entity.GetCheckConstraints()) Assert.NotEmpty(CheckExpression.Canonical(check.Sql));
            foreach (var index in entity.GetIndexes())
                if (index.GetFilter() is string filter) Assert.NotEmpty(CheckExpression.Canonical(filter));
        }
        Assert.Throws<InvalidOperationException>(() => CheckExpression.Canonical("[A]=1; DROP TABLE private"));
    }

    [Fact]
    public async Task UnexpectedEfFailuresStayFailuresWithoutPrivatePayloads()
    {
        var failure = await Assert.ThrowsAsync<SqlTestFailure>(() => SqlSafety.ProtectAsync(() =>
            Task.FromException(new DbUpdateException("secret-password;hash;Server=private", new Exception("private")))));
        Assert.Equal(-1, failure.Number);
        Assert.Null(failure.InnerException);
        Assert.DoesNotContain("secret", failure.ToString());
        Assert.DoesNotContain("private", failure.ToString());
    }

    [Theory]
    [InlineData(2601, "Unique")]
    [InlineData(547, "Integrity")]
    [InlineData(515, "Integrity")]
    [InlineData(2628, "Integrity")]
    [InlineData(18456, "SqlFailure")]
    public void ApplicationLogsOnlyCodeAndCategoryAndSuppressesRawEfFailures(int code, string category)
    {
        var sink = new Sink();
        var services = new ServiceCollection();
        services.AddLogging(logging => { Program.ConfigureLogging(logging); logging.AddProvider(sink); });
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ILoggerFactory>();
        foreach (var rawCategory in new[] { "Microsoft.EntityFrameworkCore.Update", "Microsoft.EntityFrameworkCore.Database.Command", "Microsoft.EntityFrameworkCore.Query" })
            factory.CreateLogger(rawCategory).LogError(new Exception("private-password-hash-connection"), "private-value");
        SqlErrorDiagnostics.Log(factory.CreateLogger("RequisitaPlus.Sql"), code);
        var message = Assert.Single(sink.Messages);
        Assert.Contains(code.ToString(), message); Assert.Contains(category, message);
        Assert.DoesNotContain("private", message);
    }

    private sealed class Sink : ILoggerProvider, ILogger
    {
        public List<string> Messages { get; } = [];
        public ILogger CreateLogger(string categoryName) => this;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Dispose() { }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Assert.Null(exception);
            Messages.Add(formatter(state, exception));
        }
    }
}

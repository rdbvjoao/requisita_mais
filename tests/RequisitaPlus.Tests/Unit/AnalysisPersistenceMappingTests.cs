using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using RequisitaPlus.Tests.Integration.SqlServer;
using RequisitaPlus.Web.Data;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

public sealed class AnalysisPersistenceMappingTests
{
    [Fact]
    public void InitialMigrationChecksMatchRuntimeAndExactValuesHaveCorrectUtf16Lengths()
    {
        // Offline factory blocks connections. Inspect generated operations, never execute SQL.
        using var db = new ApplicationDbContextFactory().CreateDbContext([]);
        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(Assert.Single(assembly.Migrations).Value, db.Database.ProviderName!);
        var model = db.GetService<IDesignTimeModel>().Model;
        foreach (var table in migration.UpOperations.OfType<CreateTableOperation>()
            .Where(x => x.Name is "Analise" or "Sugestao" or "OperacaoAnalise" or "Requisito"))
        {
            var entity = Assert.Single(model.GetEntityTypes(), x => x.GetTableName() == table.Name);
            Assert.Equal(entity.GetCheckConstraints().Count(), table.CheckConstraints.Count);
            foreach (var check in table.CheckConstraints)
            {
                var runtime = Assert.Single(entity.GetCheckConstraints(), x => x.Name == check.Name);
                Assert.Equal(CheckExpression.Canonical(runtime.Sql), CheckExpression.Canonical(check.Sql));
                foreach (Match match in Regex.Matches(check.Sql,
                    @"COLLATE Latin1_General_100_BIN2 = N'([^']*)' AND DATALENGTH\(\[[^\]]+\]\) = (\d+)"))
                    Assert.Equal(Encoding.Unicode.GetByteCount(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
            }
        }
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }
}

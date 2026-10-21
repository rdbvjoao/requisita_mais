using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using RequisitaPlus.Web.Data;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

public sealed class InitialMigrationTests
{
    [Fact]
    public async Task OfflineFactoryAlsoRejectsAsyncConnectionBeforeNetworkAccess()
    {
        await using var db = new ApplicationDbContextFactory().CreateDbContext([]);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => db.Database.OpenConnectionAsync(TestContext.Current.CancellationToken));
        Assert.Contains("Design-time is offline", exception.Message);
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    [Fact]
    public void OfflineFactoryAndRuntimeHaveIdenticalRelationalModels()
    {
        using var offline = new ApplicationDbContextFactory().CreateDbContext([]);
        using var services = PilotSecurityTests.Services();
        using var scope = services.CreateScope();
        var runtime = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var differ = offline.GetService<IMigrationsModelDiffer>();
        var differences = differ.GetDifferences(offline.GetService<IDesignTimeModel>().Model.GetRelationalModel(),
            runtime.GetService<IDesignTimeModel>().Model.GetRelationalModel());
        Assert.Empty(differences);
        Assert.Equal(System.Data.ConnectionState.Closed, offline.Database.GetDbConnection().State);
        Assert.Throws<InvalidOperationException>(() => offline.Database.OpenConnection());
    }

    [Fact]
    public void InitialMigrationHasApprovedStructureAndGeneratesOnlyForwardSql()
    {
        using var db = new ApplicationDbContextFactory().CreateDbContext([]);
        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(Assert.Single(assembly.Migrations).Value, db.Database.ProviderName!);
        var tables = migration.UpOperations.OfType<CreateTableOperation>().ToArray();
        Assert.Equal(11, tables.Length);
        Assert.Equal(3, tables.SelectMany(x => x.Columns).Count(x => x.IsRowVersion));
        foreach (var name in new[] { "Requisito", "Analise", "Sugestao", "OperacaoAnalise" })
        {
            var table = Assert.Single(tables, x => x.Name == name);
            Assert.All(table.ForeignKeys, fk => Assert.Equal(ReferentialAction.Restrict, fk.OnDelete));
            Assert.NotEmpty(table.CheckConstraints);
        }
        foreach (var index in new[] { "EmailIndex", "IX_Analise_IdOperacao", "IX_Analise_IdRequisito_OrdemPersistencia", "IX_Sugestao_IdAnalise_OrdemNoResultado" })
            Assert.Contains(migration.UpOperations.OfType<CreateIndexOperation>(), x => x.Name == index && x.IsUnique);
        var sql = db.GetService<IMigrator>().GenerateScript("0", assembly.Migrations.Single().Key);
        Assert.Contains("rowversion", sql);
        Assert.Contains("DATALENGTH", sql);
        Assert.Contains("Latin1_General_100_BIN2", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
        Assert.DoesNotContain("CREATE DATABASE", sql);
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }
}

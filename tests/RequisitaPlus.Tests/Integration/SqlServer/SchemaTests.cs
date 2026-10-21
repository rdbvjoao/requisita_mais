using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using RequisitaPlus.Tests.Support;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.Models;
using Xunit;

namespace RequisitaPlus.Tests.Integration.SqlServer;

[Trait("Category", "SqlServer")]
[Collection("SqlServer")]
public sealed class SchemaTests
{
    [SqlServerFact]
    public Task CatalogContainsAllMappedColumnsChecksIndexesAndForeignKeys() => SqlSafety.ProtectAsync(async () =>
    {
        await using var fixture = new SqlServerFixture();
        await fixture.OpenForTestsAsync();
        await using var db = fixture.CreateContext();
        await SqlSafety.ProtectAsync(() => db.Database.OpenConnectionAsync(), SqlStage.ChildOpen);
        await SqlSchemaAssertions.VerifyAsync(db);
    });
    [SqlServerTheory]
    [InlineData("UPDATE Requisito SET IdUsuario=-1 WHERE IdRequisito={0}", 547, "FK_Requisito_AspNetUsers_IdUsuario")]
    [InlineData("UPDATE Requisito SET Titulo=NULL WHERE IdRequisito={0}", 515, "Titulo")]
    [InlineData("UPDATE Requisito SET Titulo=N'   ' WHERE IdRequisito={0}", 547, "CK_Requisito_Titulo")]
    [InlineData("UPDATE Requisito SET Titulo=REPLICATE(N'x',151) WHERE IdRequisito={0}", 2628, "Titulo")]
    [InlineData("UPDATE Requisito SET Descricao=REPLICATE(CAST(N'x' AS nvarchar(max)),5000)+N' ' WHERE IdRequisito={0}", 547, "CK_Requisito_Descricao")]
    [InlineData("UPDATE Requisito SET Prioridade=N'alta' WHERE IdRequisito={0}", 547, "CK_Requisito_Prioridade")]
    [InlineData("UPDATE Requisito SET Prioridade=N'Alta ' WHERE IdRequisito={0}", 547, "CK_Requisito_Prioridade")]
    [InlineData("UPDATE Requisito SET Status=N'Invalido' WHERE IdRequisito={0}", 547, "CK_Requisito_Status")]
    [InlineData("UPDATE Requisito SET VersaoTextual=0 WHERE IdRequisito={0}", 547, "CK_Requisito_VersaoTextual")]
    [InlineData("UPDATE Requisito SET UltimaOrdemAnalise=-1 WHERE IdRequisito={0}", 547, "CK_Requisito_UltimaOrdemAnalise")]
    [InlineData("UPDATE Sugestao SET OrdemNoResultado=6 WHERE IdAnalise={1}", 547, "CK_Sugestao_OrdemNoResultado")]
    [InlineData("UPDATE Sugestao SET TipoProblema=N'Outro' WHERE IdAnalise={1}", 547, "CK_Sugestao_TipoProblema")]
    [InlineData("UPDATE Sugestao SET Explicacao=NCHAR(160) WHERE IdAnalise={1}", 547, "CK_Sugestao_Explicacao")]
    [InlineData("UPDATE Sugestao SET Decisao=N'Pendente ' WHERE IdAnalise={1}", 547, "CK_Sugestao_Decisao")]
    [InlineData("UPDATE Sugestao SET Decisao=N'Aplicada' WHERE IdAnalise={1}", 547, "CK_Sugestao_DecisaoTextoData")]
    [InlineData("UPDATE Sugestao SET DecididaEmUtc=SYSUTCDATETIME() WHERE IdAnalise={1}", 547, "CK_Sugestao_DecisaoTextoData")]
    [InlineData("UPDATE Sugestao SET TextoAplicado=REPLICATE(CAST(N'x' AS nvarchar(max)),5001),Decisao=N'Ajustada',DecididaEmUtc=SYSUTCDATETIME() WHERE IdAnalise={1}", 547, "CK_Sugestao_TextoAplicado")]
    [InlineData("UPDATE Analise SET OrdemPersistencia=0 WHERE IdAnalise={1}", 547, "CK_Analise_OrdemPersistencia")]
    [InlineData("UPDATE Analise SET VersaoTextualAnalisada=0 WHERE IdAnalise={1}", 547, "CK_Analise_VersaoTextualAnalisada")]
    [InlineData("UPDATE Analise SET TituloAnalisado=N'' WHERE IdAnalise={1}", 547, "CK_Analise_TituloAnalisado")]
    [InlineData("UPDATE Analise SET DescricaoAnalisada=REPLICATE(CAST(N'x' AS nvarchar(max)),5001) WHERE IdAnalise={1}", 547, "CK_Analise_DescricaoAnalisada")]
    [InlineData("UPDATE Analise SET Modelo=N'' WHERE IdAnalise={1}", 547, "CK_Analise_Modelo")]
    [InlineData("UPDATE Analise SET TokensEntrada=-1 WHERE IdAnalise={1}", 547, "CK_Analise_TokensEntrada")]
    [InlineData("UPDATE Analise SET TokensSaida=-1 WHERE IdAnalise={1}", 547, "CK_Analise_TokensSaida")]
    [InlineData("UPDATE OperacaoAnalise SET TokensEntrada=-1 WHERE IdRequisito={0}", 547, "CK_OperacaoAnalise_TokensEntrada")]
    [InlineData("UPDATE OperacaoAnalise SET TokensSaida=-1 WHERE IdRequisito={0}", 547, "CK_OperacaoAnalise_TokensSaida")]
    [InlineData("UPDATE OperacaoAnalise SET Fingerprint=REPLICATE(N'Z',64) WHERE IdRequisito={0}", 547, "CK_OperacaoAnalise_Fingerprint")]
    [InlineData("UPDATE OperacaoAnalise SET VersaoTextual=0 WHERE IdRequisito={0}", 547, "CK_OperacaoAnalise_VersaoTextual")]
    [InlineData("UPDATE OperacaoAnalise SET Estado=N'Outro' WHERE IdRequisito={0}", 547, "CK_OperacaoAnalise_Estado")]
    [InlineData("UPDATE OperacaoAnalise SET Modelo=N'' WHERE IdRequisito={0}", 547, "CK_OperacaoAnalise_Modelo")]
    [InlineData("UPDATE AspNetUsers SET Nome=N' ' WHERE Id IN (SELECT IdUsuario FROM Requisito WHERE IdRequisito={0})", 547, "CK_AspNetUsers_Nome")]
    [InlineData("UPDATE AspNetUsers SET Nome=REPLICATE(N'x',101) WHERE Id IN (SELECT IdUsuario FROM Requisito WHERE IdRequisito={0})", 2628, "Nome")]
    [InlineData("UPDATE AspNetUsers SET Email=REPLICATE(N'x',151) WHERE Id IN (SELECT IdUsuario FROM Requisito WHERE IdRequisito={0})", 2628, "Email")]
    [InlineData("DELETE FROM Analise WHERE IdAnalise={1}", 547, "FK_Sugestao_Analise_IdAnalise")]
    [InlineData("DELETE FROM OperacaoAnalise WHERE IdRequisito={0}", 547, "FK_Analise_OperacaoAnalise_IdOperacao")]
    public Task SqlRejectsInvalidWritesAndPhysicalCascade(string sql, int expectedCode, string expectedObject) => SqlSafety.ProtectAsync(async () =>
    {
        await using var fixture = new SqlServerFixture();
        await fixture.OpenForTestsAsync();
        await using var db = fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var graph = await SeedAsync(db);
        // SQL text is a fixed test case; values remain parameters.
        var error = await Assert.ThrowsAsync<SqlTestFailure>(() => SqlSafety.ProtectAsync(() => db.Database.ExecuteSqlRawAsync(sql, graph.Requirement.IdRequisito, graph.Analysis.IdAnalise)));
        Assert.Equal(expectedCode, error.Number);
        Assert.Contains(expectedObject, error.Objects);
        await transaction.RollbackAsync();
    });

    [SqlServerFact]
    public Task RowVersionRejectsStaleWriteAndPreservesCommittedText() => SqlSafety.ProtectAsync(async () =>
    {
        await using var fixture = new SqlServerFixture();
        await fixture.OpenForTestsAsync();
        await using var db = fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var graph = await SeedAsync(db);
        var original = graph.Requirement.RowVersion.ToArray();
        Assert.Equal(8, original.Length);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Requisito SET Titulo=N'Vencedor' WHERE IdRequisito={graph.Requirement.IdRequisito}");
        graph.Requirement.Titulo = "Obsoleto";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => SqlSafety.ProtectAsync(() => db.SaveChangesAsync()));
        db.ChangeTracker.Clear();
        var stored = await db.Requisitos.SingleAsync(x => x.IdRequisito == graph.Requirement.IdRequisito);
        Assert.Equal("Vencedor", stored.Titulo);
        Assert.False(original.SequenceEqual(stored.RowVersion));
        await transaction.RollbackAsync();
    });

    [SqlServerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public Task SuggestionAndOperationRowVersionsAlsoRejectStaleWrites(bool suggestion) => SqlSafety.ProtectAsync(async () =>
    {
        await using var fixture = new SqlServerFixture();
        await fixture.OpenForTestsAsync();
        await using var db = fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var graph = await SeedAsync(db);
        object entity;
        string property;
        if (suggestion)
        {
            var row = await db.Sugestoes.SingleAsync(x => x.IdAnalise == graph.Analysis.IdAnalise);
            entity = row;
            property = nameof(Sugestao.Explicacao);
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Sugestao SET Explicacao=N'Vencedor' WHERE IdSugestao={row.IdSugestao}");
        }
        else
        {
            var row = await db.OperacoesAnalise.SingleAsync(x => x.IdOperacao == graph.Analysis.IdOperacao);
            entity = row;
            property = nameof(OperacaoAnalise.MotivoFalha);
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE OperacaoAnalise SET MotivoFalha=N'Vencedor' WHERE IdOperacao={row.IdOperacao}");
        }
        var entry = db.Entry(entity);
        var original = ((byte[])entry.Property("RowVersion").CurrentValue!).ToArray();
        Assert.Equal(8, original.Length);
        entry.Property(property).CurrentValue = "Obsoleto";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => SqlSafety.ProtectAsync(() => db.SaveChangesAsync()));
        await entry.ReloadAsync();
        Assert.Equal("Vencedor", entry.Property(property).CurrentValue);
        Assert.False(original.SequenceEqual((byte[])entry.Property("RowVersion").CurrentValue!));
        await transaction.RollbackAsync();
    });

    [SqlServerTheory]
    [InlineData("EmailIndex")]
    [InlineData("IX_Analise_IdOperacao")]
    [InlineData("IX_Analise_IdRequisito_OrdemPersistencia")]
    [InlineData("IX_Sugestao_IdAnalise_OrdemNoResultado")]
    public Task UniqueIndexesRejectDuplicateValues(string indexName) => SqlSafety.ProtectAsync(async () =>
    {
        await using var fixture = new SqlServerFixture();
        await fixture.OpenForTestsAsync();
        await using var db = fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var graph = await SeedAsync(db);
        var model = db.GetService<IDesignTimeModel>().Model;
        var entity = model.GetEntityTypes().Single(x => x.GetIndexes().Any(i => i.GetDatabaseName() == indexName));
        var table = entity.GetTableName()!;
        var identifier = StoreObjectIdentifier.Table(table, entity.GetSchema());
        var columns = entity.GetProperties().Where(x => x.ValueGenerated == ValueGenerated.Never)
            .Select(x => "[" + x.GetColumnName(identifier) + "]").ToArray();
        var expressions = columns.ToArray();
        if (indexName == "EmailIndex")
            expressions[Array.IndexOf(columns, "[NormalizedUserName]")] = "N'simulado-'+CONVERT(nvarchar(36),NEWID())";
        if (indexName == "IX_Analise_IdOperacao")
            expressions[Array.IndexOf(columns, "[OrdemPersistencia]")] = "[OrdemPersistencia]+1";
        if (indexName == "IX_Analise_IdRequisito_OrdemPersistencia")
        {
            var operation = new OperacaoAnalise
            {
                IdOperacao = Guid.NewGuid(),
                IdRequisito = graph.Requirement.IdRequisito,
                IdUsuario = graph.Requirement.IdUsuario,
                VersaoTextual = 1,
                Fingerprint = new string('a', 64),
                Estado = "Recebida",
                Modelo = "qwen2.5:3b",
                CriadaEmUtc = TestData.Instant
            };
            db.Add(operation);
            await db.SaveChangesAsync();
            expressions[Array.IndexOf(columns, "[IdOperacao]")] = "'" + operation.IdOperacao.ToString("D") + "'";
        }
        // Names originate solely in the approved model, never request input.
        var sql = $"INSERT INTO [{table}] ({string.Join(',', columns)}) SELECT {string.Join(',', expressions)} FROM [{table}] WHERE [{entity.FindPrimaryKey()!.Properties.Single().GetColumnName(identifier)}]={{0}}";
        var id = table == "AspNetUsers" ? graph.Requirement.IdUsuario : table == "Analise" ? graph.Analysis.IdAnalise :
            await db.Sugestoes.Where(x => x.IdAnalise == graph.Analysis.IdAnalise).Select(x => x.IdSugestao).SingleAsync();
        var exception = await Assert.ThrowsAsync<SqlTestFailure>(() => SqlSafety.ProtectAsync(() => db.Database.ExecuteSqlRawAsync(sql, id)));
        Assert.Equal(2601, exception.Number);
        Assert.Contains(indexName, exception.Objects);
        await transaction.RollbackAsync();
    });

    [SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ValidMinimumAndMaximumBoundariesRoundTripWithoutTruncation(bool maximum) => SqlSafety.ProtectAsync(async () =>
    {
        await using var fixture = new SqlServerFixture();
        await fixture.OpenForTestsAsync();
        await using var db = fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var graph = await SeedAsync(db);
        var name = maximum ? new string('n', 99) + " " : "N";
        var email = maximum ? new string('a', 62) + "@" + new string('b', 60) + "." + new string('c', 26) : "a@example.invalid";
        var title = maximum ? new string('t', 149) + " " : "T";
        var description = maximum ? string.Concat(Enumerable.Repeat("😀", 2499)) + "x " : "D";
        var explanation = maximum ? new string('e', 499) + " " : "E";
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE AspNetUsers SET Nome={name},Email={email},NormalizedEmail={email.ToUpperInvariant()},UserName={email},NormalizedUserName={email.ToUpperInvariant()} WHERE Id={graph.Requirement.IdUsuario}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Requisito SET Titulo={title},Descricao={description},VersaoTextual=1,UltimaOrdemAnalise=0 WHERE IdRequisito={graph.Requirement.IdRequisito}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Analise SET TituloAnalisado={title},DescricaoAnalisada={description},TokensEntrada=NULL,TokensSaida=0 WHERE IdAnalise={graph.Analysis.IdAnalise}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Sugestao SET Explicacao={explanation},TextoSugerido={new string('s', 6000)},TextoAplicado={description},Decisao=N'Ajustada',DecididaEmUtc=SYSUTCDATETIME() WHERE IdAnalise={graph.Analysis.IdAnalise}");
        db.ChangeTracker.Clear();
        var user = await db.Users.SingleAsync(x => x.Id == graph.Requirement.IdUsuario);
        var requirement = await db.Requisitos.SingleAsync(x => x.IdRequisito == graph.Requirement.IdRequisito);
        var analysis = await db.Analises.SingleAsync(x => x.IdAnalise == graph.Analysis.IdAnalise);
        var suggestion = await db.Sugestoes.SingleAsync(x => x.IdAnalise == graph.Analysis.IdAnalise);
        Assert.True(user.Nome == name && user.Email == email && user.NormalizedEmail == email.ToUpperInvariant());
        Assert.True(requirement.Titulo == title && requirement.Descricao == description);
        Assert.True(analysis.TituloAnalisado == title && analysis.DescricaoAnalisada == description);
        Assert.True(suggestion.Explicacao == explanation && suggestion.TextoAplicado == description && suggestion.TextoSugerido!.Length == 6000);
        Assert.Null(analysis.TokensEntrada); Assert.Equal(0, analysis.TokensSaida);
        await transaction.RollbackAsync();
    });

    [SqlServerFact]
    public Task SupplementaryUnicodeAndTrailingSpaceCannotBypassDescriptionLimit() => SqlSafety.ProtectAsync(async () =>
    {
        await using var fixture = new SqlServerFixture();
        await fixture.OpenForTestsAsync();
        await using var db = fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var graph = await SeedAsync(db);
        var value = string.Concat(Enumerable.Repeat("😀", 2500)) + " ";
        var error = await Assert.ThrowsAsync<SqlTestFailure>(() => SqlSafety.ProtectAsync(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Requisito SET Descricao={value} WHERE IdRequisito={graph.Requirement.IdRequisito}")));
        Assert.Equal(547, error.Number); Assert.Contains("CK_Requisito_Descricao", error.Objects);
        await transaction.RollbackAsync();
    });

    [SqlServerFact]
    public Task EmptyOperationIdAndEachParentDeletionFailOnTheirSpecificConstraint() => SqlSafety.ProtectAsync(async () =>
    {
        await using var fixture = new SqlServerFixture();
        await fixture.OpenForTestsAsync();
        await using var db = fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var graph = await SeedAsync(db, full: false);
        var userError = await Assert.ThrowsAsync<SqlTestFailure>(() => SqlSafety.ProtectAsync(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM AspNetUsers WHERE Id={graph.Requirement.IdUsuario}")));
        Assert.Equal(547, userError.Number); Assert.Contains("FK_Requisito_AspNetUsers_IdUsuario", userError.Objects);
        var zero = await Assert.ThrowsAsync<SqlTestFailure>(() => SqlSafety.ProtectAsync(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO OperacaoAnalise (IdOperacao,IdUsuario,IdRequisito,VersaoTextual,Fingerprint,Estado,CriadaEmUtc,Modelo,CustoApi) VALUES ({Guid.Empty},{graph.Requirement.IdUsuario},{graph.Requirement.IdRequisito},1,{new string('a', 64)},N'Recebida',SYSUTCDATETIME(),N'qwen2.5:3b',0)")));
        Assert.Equal(547, zero.Number); Assert.Contains("CK_OperacaoAnalise_IdOperacao", zero.Objects);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO OperacaoAnalise (IdOperacao,IdUsuario,IdRequisito,VersaoTextual,Fingerprint,Estado,CriadaEmUtc,Modelo,CustoApi) VALUES ({Guid.NewGuid()},{graph.Requirement.IdUsuario},{graph.Requirement.IdRequisito},1,{new string('a', 64)},N'Recebida',SYSUTCDATETIME(),N'qwen2.5:3b',0)");
        var parentError = await Assert.ThrowsAsync<SqlTestFailure>(() => SqlSafety.ProtectAsync(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Requisito WHERE IdRequisito={graph.Requirement.IdRequisito}")));
        Assert.Equal(547, parentError.Number); Assert.Contains("FK_OperacaoAnalise_Requisito_IdRequisito", parentError.Objects);
        await transaction.RollbackAsync();
    });

    internal static async Task<(Requisito Requirement, Analise Analysis)> SeedAsync(ApplicationDbContext db, bool full = true)
    {
        var data = new TestData();
        var user = data.Users[0];
        user.Id = 0;
        user.Email = user.UserName = $"simulado-{Guid.NewGuid():N}@example.invalid";
        user.NormalizedEmail = user.NormalizedUserName = user.Email.ToUpperInvariant();
        var requirement = data.Requirements[0];
        requirement.IdRequisito = requirement.IdUsuario = 0;
        var operation = data.Operations[0];
        operation.IdOperacao = Guid.NewGuid();
        operation.IdRequisito = operation.IdUsuario = 0;
        var analysis = data.Analyses[0];
        analysis.IdAnalise = analysis.IdRequisito = 0;
        analysis.IdOperacao = operation.IdOperacao;
        var suggestion = data.Suggestions[0];
        suggestion.IdSugestao = suggestion.IdAnalise = 0;
        if (full) { db.Add(operation); db.Add(suggestion); }
        else db.Add(requirement);
        await db.SaveChangesAsync();
        return (requirement, analysis);
    }
}

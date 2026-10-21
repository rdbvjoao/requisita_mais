using System.Data.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.Models;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

// SQL Server provider metadata only: no server, connection string or database exists
// in this fixture. The interceptor also rejects any accidental attempt to connect.
[Trait("Category", "Unit")]
public sealed class ApplicationDbContextModelTests : IDisposable
{
    private readonly RejectConnections guard = new();
    private readonly ApplicationDbContext context;
    private readonly IModel model;

    public ApplicationDbContextModelTests()
    {
        context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            // Metadata-only options omit Identity UI's DI options. Isolate their
            // model cache so they cannot replace the runtime/design-time model.
            .UseSqlServer().EnableServiceProviderCaching(false).AddInterceptors(guard).Options);
        model = context.GetService<IDesignTimeModel>().Model;
    }

    public void Dispose()
    {
        Assert.Equal(0, guard.Attempts);
        context.Dispose();
    }

    [Fact]
    public void IdentityUsesIntegerKeysAndKeepsItsAuxiliaryTables()
    {
        Assert.IsAssignableFrom<IdentityDbContext<ApplicationUser, IdentityRole<int>, int>>(context);
        Assert.Equal("AspNetUsers", Entity<ApplicationUser>().GetTableName());
        Assert.Equal(typeof(int), Entity<ApplicationUser>().FindPrimaryKey()!.Properties.Single().ClrType);
        Assert.NotNull(model.FindEntityType(typeof(IdentityRole<int>)));
        Assert.NotNull(model.FindEntityType(typeof(IdentityUserClaim<int>)));
        Assert.NotNull(model.FindEntityType(typeof(IdentityUserLogin<int>)));
        Assert.NotNull(model.FindEntityType(typeof(IdentityUserToken<int>)));
        Assert.NotNull(model.FindEntityType(typeof(IdentityUserRole<int>)));
        Assert.NotNull(model.FindEntityType(typeof(IdentityRoleClaim<int>)));
        AssertUnique<ApplicationUser>("NormalizedEmail");
        AssertUnique<ApplicationUser>("NormalizedUserName");
        Assert.Null(Property<ApplicationUser>("PasswordHash").GetMaxLength());
    }

    [Theory]
    [InlineData(typeof(Requisito), "IdRequisito", "Requisito")]
    [InlineData(typeof(Analise), "IdAnalise", "Analise")]
    [InlineData(typeof(Sugestao), "IdSugestao", "Sugestao")]
    public void DomainIntegerKeysAreIdentityColumns(Type type, string keyName, string table)
    {
        var entity = model.FindEntityType(type)!;
        var key = Assert.Single(entity.FindPrimaryKey()!.Properties);
        Assert.Equal(keyName, key.Name);
        Assert.Equal(typeof(int), key.ClrType);
        Assert.Equal(ValueGenerated.OnAdd, key.ValueGenerated);
        Assert.Equal(SqlServerValueGenerationStrategy.IdentityColumn, key.GetValueGenerationStrategy());
        Assert.Equal(table, entity.GetTableName());
        Assert.DoesNotContain(entity.GetProperties(), property => property.IsShadowProperty());
    }

    [Fact]
    public void OperationGuidIsSuppliedByTheCallerAndNeverGeneratedByEfOrSql()
    {
        var property = Property<OperacaoAnalise>("IdOperacao");
        Assert.Equal(property, Assert.Single(Entity<OperacaoAnalise>().FindPrimaryKey()!.Properties));
        Assert.Equal("uniqueidentifier", property.GetColumnType());
        Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
        Assert.Null(property.GetDefaultValueSql());
        Assert.Null(property.FindAnnotation(RelationalAnnotationNames.DefaultValue));
        Assert.Equal(ValueGenerated.Never, Property<Analise>("IdOperacao").ValueGenerated);
        Assert.False((bool)Property<OperacaoAnalise>("TerminoComprovado").GetDefaultValue()!);
    }

    [Fact]
    public void DomainRelationshipsAreRequiredAndPreventPhysicalCascades()
    {
        AssertForeignKey<Requisito, ApplicationUser>("IdUsuario", false);
        AssertForeignKey<Analise, Requisito>("IdRequisito", false);
        AssertForeignKey<Analise, OperacaoAnalise>("IdOperacao", true);
        AssertForeignKey<Sugestao, Analise>("IdAnalise", false);
        AssertForeignKey<OperacaoAnalise, ApplicationUser>("IdUsuario", false);
        AssertForeignKey<OperacaoAnalise, Requisito>("IdRequisito", false);
        Assert.Equal(6, new[] { Entity<Requisito>(), Entity<Analise>(), Entity<Sugestao>(), Entity<OperacaoAnalise>() }
            .SelectMany(entity => entity.GetForeignKeys()).Count());
    }

    [Fact]
    public void ApprovedIndexesPreserveUniquenessAndActiveAnalysisOrdering()
    {
        AssertUnique<Analise>("IdOperacao");
        AssertUnique<Analise>("IdRequisito", "OrdemPersistencia");
        AssertUnique<Sugestao>("IdAnalise", "OrdemNoResultado");
        Assert.False(Index<Requisito>("IdUsuario", "Status", "IdRequisito").IsUnique);
        var active = Index<Analise>("IdRequisito", "VersaoTextualAnalisada", "OrdemPersistencia");
        Assert.False(active.IsUnique);
        Assert.Equal(new[] { false, false, true }, active.IsDescending);
    }

    [Theory]
    [InlineData(typeof(ApplicationUser), "Nome", "nvarchar(100)", 100, false)]
    [InlineData(typeof(ApplicationUser), "Email", "nvarchar(150)", 150, false)]
    [InlineData(typeof(ApplicationUser), "NormalizedEmail", "nvarchar(150)", 150, false)]
    [InlineData(typeof(ApplicationUser), "UserName", "nvarchar(150)", 150, false)]
    [InlineData(typeof(ApplicationUser), "NormalizedUserName", "nvarchar(150)", 150, false)]
    [InlineData(typeof(Requisito), "Titulo", "nvarchar(150)", 150, false)]
    [InlineData(typeof(Requisito), "Descricao", "nvarchar(max)", 5000, false)]
    [InlineData(typeof(Requisito), "Prioridade", "nvarchar(5)", 5, false)]
    [InlineData(typeof(Requisito), "Status", "nvarchar(20)", 20, false)]
    [InlineData(typeof(Analise), "TituloAnalisado", "nvarchar(150)", 150, false)]
    [InlineData(typeof(Analise), "DescricaoAnalisada", "nvarchar(max)", 5000, false)]
    [InlineData(typeof(Sugestao), "TipoProblema", "nvarchar(50)", 50, false)]
    [InlineData(typeof(Sugestao), "Explicacao", "nvarchar(500)", 500, false)]
    [InlineData(typeof(Sugestao), "TextoAplicado", "nvarchar(max)", 5000, true)]
    [InlineData(typeof(OperacaoAnalise), "Fingerprint", "nvarchar(64)", 64, false)]
    public void TextColumnsHaveApprovedUnicodeTypesLimitsAndNullability(Type type, string name, string sqlType, int limit, bool nullable)
    {
        var property = model.FindEntityType(type)!.FindProperty(name)!;
        Assert.Equal(sqlType, property.GetColumnType());
        Assert.Equal(limit, property.GetMaxLength());
        Assert.Equal(nullable, property.IsNullable);
    }

    [Theory]
    [InlineData(typeof(Analise), "Modelo", false)]
    [InlineData(typeof(Analise), "DigestModelo", true)]
    [InlineData(typeof(Analise), "VersaoPrompt", true)]
    [InlineData(typeof(Sugestao), "TextoSugerido", true)]
    [InlineData(typeof(OperacaoAnalise), "Modelo", false)]
    [InlineData(typeof(OperacaoAnalise), "MotivoFalha", true)]
    [InlineData(typeof(OperacaoAnalise), "EvidenciaRecuperacao", true)]
    public void UnboundedMetadataAndOriginalProposalGainNoInventedLengthLimit(Type type, string name, bool nullable)
    {
        var property = model.FindEntityType(type)!.FindProperty(name)!;
        Assert.Equal("nvarchar(max)", property.GetColumnType());
        Assert.Null(property.GetMaxLength());
        Assert.Equal(nullable, property.IsNullable);
    }

    [Theory]
    [InlineData(typeof(Requisito))]
    [InlineData(typeof(Sugestao))]
    [InlineData(typeof(OperacaoAnalise))]
    public void RowVersionIsDatabaseGeneratedConcurrencyToken(Type type)
    {
        var property = model.FindEntityType(type)!.FindProperty("RowVersion")!;
        Assert.Equal(typeof(byte[]), property.ClrType);
        Assert.Equal("rowversion", property.GetColumnType());
        Assert.True(property.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
        Assert.False(property.IsNullable);
    }

    [Theory]
    [InlineData(typeof(Analise))]
    [InlineData(typeof(OperacaoAnalise))]
    public void CostPrecisionAndUnknownTokenCountsArePreserved(Type type)
    {
        var entity = model.FindEntityType(type)!;
        var cost = entity.FindProperty("CustoApi")!;
        Assert.Equal("decimal(18,6)", cost.GetColumnType());
        Assert.Equal(18, cost.GetPrecision());
        Assert.Equal(6, cost.GetScale());
        foreach (var name in new[] { "TokensEntrada", "TokensSaida" })
        {
            var tokens = entity.FindProperty(name)!;
            Assert.True(tokens.IsNullable);
            Assert.Equal("int", tokens.GetColumnType());
            Assert.Null(tokens.FindAnnotation(RelationalAnnotationNames.DefaultValue));
            Assert.Contains($"[{name}] IS NULL OR [{name}] >= 0", entity.GetCheckConstraints().Select(check => check.Sql));
        }
    }

    [Fact]
    public void DateMappingsPreserveNullabilityAndUtcWithoutReadingAClock()
    {
        var dates = new (Type Entity, string Name, bool Nullable)[]
        {
            (typeof(Requisito), "DataCadastroUtc", false), (typeof(Requisito), "DataAtualizacaoUtc", true),
            (typeof(Analise), "DataAnaliseUtc", false), (typeof(Analise), "PersistidaEmUtc", false),
            (typeof(Analise), "RevisaoConcluidaEmUtc", true), (typeof(Sugestao), "DecididaEmUtc", true),
            (typeof(OperacaoAnalise), "CriadaEmUtc", false), (typeof(OperacaoAnalise), "EnvioRegistradoEmUtc", true),
            (typeof(OperacaoAnalise), "EncerradaEmUtc", true), (typeof(OperacaoAnalise), "PrazoExcedidoEmUtc", true)
        };
        var utc = new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
        foreach (var (type, name, nullable) in dates)
        {
            var property = model.FindEntityType(type)!.FindProperty(name)!;
            Assert.Equal("datetime2", property.GetColumnType());
            Assert.Equal(nullable, property.IsNullable);
            Assert.Null(property.GetDefaultValueSql());
            var converter = property.GetValueConverter()!;
            Assert.Equal(utc, converter.ConvertToProvider(utc));
            Assert.Equal(DateTimeKind.Utc, ((DateTime)converter.ConvertFromProvider(DateTime.SpecifyKind(utc, DateTimeKind.Unspecified))!).Kind);
            Assert.Throws<InvalidOperationException>(() => converter.ConvertToProvider(DateTime.SpecifyKind(utc, DateTimeKind.Local)));
            Assert.Throws<InvalidOperationException>(() => converter.ConvertToProvider(DateTime.SpecifyKind(utc, DateTimeKind.Unspecified)));
            if (nullable)
            {
                Assert.Null(converter.ConvertToProvider(null));
                Assert.Null(converter.ConvertFromProvider(null));
            }
        }
    }

    [Theory]
    [InlineData(typeof(Requisito), "CK_Requisito_Descricao", "Descricao", 10000)]
    [InlineData(typeof(Analise), "CK_Analise_DescricaoAnalisada", "DescricaoAnalisada", 10000)]
    [InlineData(typeof(Sugestao), "CK_Sugestao_Explicacao", "Explicacao", 1000)]
    public void TextChecksUseUtf16BytesIncludingTrailingSpacesAndRejectUnicodeWhitespace(Type type, string checkName, string column, int bytes)
    {
        var sql = Check(type, checkName);
        Assert.Contains($"DATALENGTH([{column}]) <= {bytes}", sql);
        Assert.DoesNotContain("LEN(", sql);
        Assert.Contains("NCHAR(32)", sql);
        Assert.Contains("NCHAR(160)", sql);
        Assert.Contains("NCHAR(8195)", sql);
        Assert.Contains("COLLATE Latin1_General_100_BIN2", sql);
    }

    [Fact]
    public void EnumeratedChecksRequireExactSpellingAndLengthIndependentlyOfDatabaseCollation()
    {
        AssertValues<Requisito>("CK_Requisito_Prioridade", "Alta", "Média", "Baixa");
        AssertValues<Requisito>("CK_Requisito_Status", "Rascunho", "Analisado", "Revisado", "Arquivado");
        AssertValues<Sugestao>("CK_Sugestao_TipoProblema", "Ambiguidade", "Termo vago", "Falta de clareza", "Falta de especificidade");
        AssertValues<Sugestao>("CK_Sugestao_Decisao", "Pendente", "Aplicada", "Ajustada", "Descartada");
        AssertValues<OperacaoAnalise>("CK_OperacaoAnalise_Estado", "Recebida", "Enviada", "ResultadoDisponivel", "Persistida", "FalhaEncerrada", "Incerta");
        Assert.Null(Property<Requisito>("Prioridade").FindAnnotation(RelationalAnnotationNames.DefaultValue));
    }

    [Fact]
    public void NumericAndFingerprintChecksArePresentWithoutAutomaticHashGeneration()
    {
        Assert.Equal("[VersaoTextual] > 0", Check(typeof(Requisito), "CK_Requisito_VersaoTextual"));
        Assert.Equal("[UltimaOrdemAnalise] >= 0", Check(typeof(Requisito), "CK_Requisito_UltimaOrdemAnalise"));
        Assert.Equal("[VersaoTextualAnalisada] > 0", Check(typeof(Analise), "CK_Analise_VersaoTextualAnalisada"));
        Assert.Equal("[OrdemPersistencia] > 0", Check(typeof(Analise), "CK_Analise_OrdemPersistencia"));
        Assert.Equal("[VersaoTextual] > 0", Check(typeof(OperacaoAnalise), "CK_OperacaoAnalise_VersaoTextual"));
        Assert.Equal("[OrdemNoResultado] BETWEEN 1 AND 5", Check(typeof(Sugestao), "CK_Sugestao_OrdemNoResultado"));
        var hash = Check(typeof(OperacaoAnalise), "CK_OperacaoAnalise_Fingerprint");
        Assert.Contains("DATALENGTH([Fingerprint]) = 128", hash);
        Assert.Contains("COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^0-9A-Fa-f]%'", hash);
        Assert.Equal(ValueGenerated.Never, Property<OperacaoAnalise>("Fingerprint").ValueGenerated);
    }

    [Fact]
    public void DecisionCheckKeepsProposedTextSeparateAndRequiresFinalTextAndDecisionDate()
    {
        var sql = Check(typeof(Sugestao), "CK_Sugestao_DecisaoTextoData");
        Assert.Contains("[Decisao] = N'Pendente' AND [TextoAplicado] IS NULL AND [DecididaEmUtc] IS NULL", sql);
        Assert.Contains("[Decisao] = N'Descartada' AND [TextoAplicado] IS NULL AND [DecididaEmUtc] IS NOT NULL", sql);
        Assert.Contains("[Decisao] IN (N'Aplicada', N'Ajustada') AND [TextoAplicado] IS NOT NULL", sql);
        Assert.Contains("[DecididaEmUtc] IS NOT NULL", sql);
        Assert.DoesNotContain("TextoSugerido", sql);
        Assert.Contains("DATALENGTH([TextoAplicado]) <= 10000", Check(typeof(Sugestao), "CK_Sugestao_TextoAplicado"));
        Assert.Null(Property<Sugestao>("TextoAplicado").GetComputedColumnSql());
    }

    private IEntityType Entity<T>() => model.FindEntityType(typeof(T))!;
    private IProperty Property<T>(string name) => Entity<T>().FindProperty(name)!;
    private IIndex Index<T>(params string[] names) => Assert.Single(Entity<T>().GetIndexes(),
        index => index.Properties.Select(property => property.Name).SequenceEqual(names));
    private void AssertUnique<T>(params string[] names) => Assert.True(Index<T>(names).IsUnique);
    private string Check(Type type, string name) => model.FindEntityType(type)!.GetCheckConstraints().Single(check => check.Name == name).Sql;
    private void AssertValues<T>(string checkName, params string[] values)
    {
        var sql = Check(typeof(T), checkName);
        foreach (var value in values)
            Assert.Contains($"N'{value}'", sql);
        Assert.Contains("COLLATE Latin1_General_100_BIN2", sql);
        Assert.Contains("DATALENGTH(", sql);
        Assert.Equal(values.Length, sql.Split(" COLLATE ").Length - 1);
    }
    private void AssertForeignKey<TDependent, TPrincipal>(string name, bool unique)
    {
        var foreignKey = Assert.Single(Entity<TDependent>().GetForeignKeys(), key => key.Properties.Single().Name == name);
        Assert.Equal(typeof(TPrincipal), foreignKey.PrincipalEntityType.ClrType);
        Assert.True(foreignKey.IsRequired);
        Assert.Equal(unique, foreignKey.IsUnique);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    private sealed class RejectConnections : DbConnectionInterceptor
    {
        public int Attempts { get; private set; }
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            Attempts++;
            throw new InvalidOperationException("Database connections are forbidden in model metadata tests.");
        }
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            Attempts++;
            throw new InvalidOperationException("Database connections are forbidden in model metadata tests.");
        }
    }
}

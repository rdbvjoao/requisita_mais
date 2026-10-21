using System.ComponentModel.DataAnnotations;
using RequisitaPlus.Web.Models;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class AnaliseModelTests
{
    [Fact]
    public void ApprovedPropertiesHaveExpectedTypesWithoutFutureEntities()
    {
        var expected = new Dictionary<string, Type>
        {
            [nameof(Analise.IdAnalise)] = typeof(int),
            [nameof(Analise.IdRequisito)] = typeof(int),
            [nameof(Analise.Requisito)] = typeof(Requisito),
            [nameof(Analise.IdOperacao)] = typeof(Guid),
            [nameof(Analise.VersaoTextualAnalisada)] = typeof(long),
            [nameof(Analise.TituloAnalisado)] = typeof(string),
            [nameof(Analise.DescricaoAnalisada)] = typeof(string),
            [nameof(Analise.DataAnaliseUtc)] = typeof(DateTime),
            [nameof(Analise.PersistidaEmUtc)] = typeof(DateTime),
            [nameof(Analise.OrdemPersistencia)] = typeof(long),
            [nameof(Analise.Modelo)] = typeof(string),
            [nameof(Analise.DigestModelo)] = typeof(string),
            [nameof(Analise.VersaoPrompt)] = typeof(string),
            [nameof(Analise.TokensEntrada)] = typeof(int?),
            [nameof(Analise.TokensSaida)] = typeof(int?),
            [nameof(Analise.CustoApi)] = typeof(decimal),
            [nameof(Analise.RevisaoConcluidaEmUtc)] = typeof(DateTime?)
        };
        var properties = typeof(Analise).GetProperties();
        Assert.Equal(expected.Count, properties.Length);
        foreach (var property in properties)
            Assert.Equal(expected[property.Name], property.PropertyType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    [InlineData("\u2003\u00a0")]
    public void SnapshotsAndModelIdentifierRejectMissingOrWhitespaceText(string? value)
    {
        var analise = ValidModel();
        analise.TituloAnalisado = value!;
        analise.DescricaoAnalisada = value!;
        analise.Modelo = value!;
        var errors = Validate(analise);
        foreach (var field in new[] { "TituloAnalisado", "DescricaoAnalisada", "Modelo" })
            Assert.Contains(errors, error => error.MemberNames.Contains(field));
    }

    [Theory]
    [InlineData(nameof(Analise.TituloAnalisado), 150, true)]
    [InlineData(nameof(Analise.TituloAnalisado), 151, false)]
    [InlineData(nameof(Analise.DescricaoAnalisada), 5000, true)]
    [InlineData(nameof(Analise.DescricaoAnalisada), 5001, false)]
    public void SnapshotLimitsCountUtf16AndNeverTruncate(string field, int length, bool valid)
    {
        var analise = ValidModel();
        var value = new string('á', length - 2) + "😀";
        var property = typeof(Analise).GetProperty(field)!;
        property.SetValue(analise, value);
        Assert.Equal(length, value.Length);
        Assert.Equal(valid, Validate(analise).Count == 0);
        Assert.Equal(value, property.GetValue(analise));
    }

    [Fact]
    public void SnapshotsPreserveExactUnicodeAndDoNotFollowChangesToTheRequirement()
    {
        var requisito = new Requisito { IdRequisito = 7, Titulo = "  Revisão — 李 😀  ", Descricao = "Descrição\r\ncom acentuação.  " };
        var analise = ValidModel();
        analise.IdRequisito = requisito.IdRequisito;
        analise.Requisito = requisito;
        analise.TituloAnalisado = requisito.Titulo;
        analise.DescricaoAnalisada = requisito.Descricao;
        requisito.Titulo = "Novo título";
        requisito.Descricao = "Nova descrição";

        Assert.Empty(Validate(analise));
        Assert.Equal("  Revisão — 李 😀  ", analise.TituloAnalisado);
        Assert.Equal("Descrição\r\ncom acentuação.  ", analise.DescricaoAnalisada);
        Assert.Same(requisito, analise.Requisito);
        Assert.Equal(requisito.IdRequisito, analise.IdRequisito);
    }

    [Theory]
    [InlineData(0, 1, false)]
    [InlineData(-1, 1, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, -1, false)]
    [InlineData(1, 1, true)]
    [InlineData(long.MaxValue, long.MaxValue, true)]
    public void TextVersionAndPersistenceOrderMustBePositive(long version, long order, bool valid)
    {
        var analise = ValidModel();
        analise.VersaoTextualAnalisada = version;
        analise.OrdemPersistencia = order;
        Assert.Equal(valid, Validate(analise).Count == 0);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(int.MaxValue, true)]
    [InlineData(-1, false)]
    public void TokensMayBeUnknownButCannotBeNegativeWhenKnown(int? value, bool valid)
    {
        var analise = ValidModel();
        analise.TokensEntrada = value;
        analise.TokensSaida = value;
        Assert.Equal(valid, Validate(analise).Count == 0);
        Assert.Equal(value, analise.TokensEntrada);
        Assert.Equal(value, analise.TokensSaida);
    }

    [Theory]
    [InlineData(nameof(Analise.DataAnaliseUtc), DateTimeKind.Utc, true)]
    [InlineData(nameof(Analise.DataAnaliseUtc), DateTimeKind.Local, false)]
    [InlineData(nameof(Analise.DataAnaliseUtc), DateTimeKind.Unspecified, false)]
    [InlineData(nameof(Analise.PersistidaEmUtc), DateTimeKind.Utc, true)]
    [InlineData(nameof(Analise.PersistidaEmUtc), DateTimeKind.Local, false)]
    [InlineData(nameof(Analise.PersistidaEmUtc), DateTimeKind.Unspecified, false)]
    [InlineData(nameof(Analise.RevisaoConcluidaEmUtc), DateTimeKind.Utc, true)]
    [InlineData(nameof(Analise.RevisaoConcluidaEmUtc), DateTimeKind.Local, false)]
    [InlineData(nameof(Analise.RevisaoConcluidaEmUtc), DateTimeKind.Unspecified, false)]
    public void DatesRequireUtcWithoutConversion(string field, DateTimeKind kind, bool valid)
    {
        var analise = ValidModel();
        var value = new DateTime(2026, 10, 18, 12, 0, 0, kind);
        var property = typeof(Analise).GetProperty(field)!;
        property.SetValue(analise, value);
        Assert.Equal(valid, Validate(analise).Count == 0);
        Assert.Equal(kind, ((DateTime)property.GetValue(analise)!).Kind);
    }

    [Fact]
    public void EmptyOperationIsRejectedWithoutGeneratingAnIdentifier()
    {
        var analise = ValidModel();
        analise.IdOperacao = Guid.Empty;
        Assert.Contains(Validate(analise), error => error.MemberNames.Contains(nameof(Analise.IdOperacao)));
        Assert.Equal(Guid.Empty, analise.IdOperacao);
    }

    [Fact]
    public void DefaultsDoNotInventTokensDatesMetadataOrHumanReview()
    {
        var analise = new Analise();
        Assert.Null(analise.TokensEntrada);
        Assert.Null(analise.TokensSaida);
        Assert.Null(analise.DigestModelo);
        Assert.Null(analise.VersaoPrompt);
        Assert.Null(analise.RevisaoConcluidaEmUtc);
        Assert.Equal(default, analise.DataAnaliseUtc);
        Assert.Equal(default, analise.PersistidaEmUtc);
        Assert.Equal(0m, analise.CustoApi);
    }

    [Fact]
    public void CompletionPersistenceAndHumanReviewRemainSeparate()
    {
        var analise = ValidModel();
        var completed = analise.DataAnaliseUtc;
        analise.PersistidaEmUtc = completed.AddSeconds(2);
        Assert.Empty(Validate(analise));
        Assert.Equal(completed, analise.DataAnaliseUtc);
        Assert.NotEqual(analise.DataAnaliseUtc, analise.PersistidaEmUtc);
        Assert.Null(analise.RevisaoConcluidaEmUtc);
    }

    [Fact]
    public void TechnicalIdentifiersHaveNoInventedMaximumOrFormatAndCostIsNotCalculated()
    {
        var analise = ValidModel();
        analise.Modelo = new string('m', 6000);
        analise.DigestModelo = "identificador técnico sem formato imposto";
        analise.VersaoPrompt = new string('p', 6000);
        Assert.Empty(Validate(analise));
        Assert.Equal(6000, analise.Modelo.Length);
        Assert.Equal(6000, analise.VersaoPrompt.Length);
        Assert.Equal(0m, analise.CustoApi);
    }

    private static List<ValidationResult> Validate(Analise analise)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(analise, new ValidationContext(analise), errors, validateAllProperties: true);
        return errors;
    }

    private static Analise ValidModel() => new()
    {
        IdOperacao = Guid.Parse("8e2ba00b-81f4-4c8d-9ae0-b9edc31d04b6"),
        VersaoTextualAnalisada = 1,
        TituloAnalisado = "Título analisado",
        DescricaoAnalisada = "Descrição analisada.",
        Modelo = "qwen2.5:3b",
        DataAnaliseUtc = new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc),
        PersistidaEmUtc = new DateTime(2026, 10, 18, 12, 0, 1, DateTimeKind.Utc),
        OrdemPersistencia = 1
    };
}

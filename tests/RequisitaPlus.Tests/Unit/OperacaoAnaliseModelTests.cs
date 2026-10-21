using System.ComponentModel.DataAnnotations;
using RequisitaPlus.Web.Models;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class OperacaoAnaliseModelTests
{
    [Fact]
    public void ApprovedPropertiesHaveExpectedTypesWithoutPayloadOrAnalysisIdentifier()
    {
        var expected = new Dictionary<string, Type>
        {
            [nameof(OperacaoAnalise.IdOperacao)] = typeof(Guid),
            [nameof(OperacaoAnalise.IdUsuario)] = typeof(int),
            [nameof(OperacaoAnalise.Usuario)] = typeof(ApplicationUser),
            [nameof(OperacaoAnalise.IdRequisito)] = typeof(int),
            [nameof(OperacaoAnalise.Requisito)] = typeof(Requisito),
            [nameof(OperacaoAnalise.VersaoTextual)] = typeof(long),
            [nameof(OperacaoAnalise.Fingerprint)] = typeof(string),
            [nameof(OperacaoAnalise.Estado)] = typeof(string),
            [nameof(OperacaoAnalise.CriadaEmUtc)] = typeof(DateTime),
            [nameof(OperacaoAnalise.EnvioRegistradoEmUtc)] = typeof(DateTime?),
            [nameof(OperacaoAnalise.EncerradaEmUtc)] = typeof(DateTime?),
            [nameof(OperacaoAnalise.PrazoExcedidoEmUtc)] = typeof(DateTime?),
            [nameof(OperacaoAnalise.TerminoComprovado)] = typeof(bool),
            [nameof(OperacaoAnalise.MotivoFalha)] = typeof(string),
            [nameof(OperacaoAnalise.EvidenciaRecuperacao)] = typeof(string),
            [nameof(OperacaoAnalise.Modelo)] = typeof(string),
            [nameof(OperacaoAnalise.TokensEntrada)] = typeof(int?),
            [nameof(OperacaoAnalise.TokensSaida)] = typeof(int?),
            [nameof(OperacaoAnalise.CustoApi)] = typeof(decimal),
            [nameof(OperacaoAnalise.RowVersion)] = typeof(byte[])
        };
        var properties = typeof(OperacaoAnalise).GetProperties();
        Assert.Equal(expected.Count, properties.Length);
        foreach (var property in properties)
            Assert.Equal(expected[property.Name], property.PropertyType);
    }

    [Fact]
    public void OperationIdentifierIsRequiredAndNeverGeneratedByTheEntity()
    {
        var operation = ValidModel();
        operation.IdOperacao = Guid.Empty;
        Assert.Contains(Validate(operation), error => error.MemberNames.Contains(nameof(OperacaoAnalise.IdOperacao)));
        Assert.Equal(Guid.Empty, operation.IdOperacao);
    }

    [Fact]
    public void OwnerAndRequirementLinksRemainStructuralWithoutAuthorizationOrMutation()
    {
        var owner = new ApplicationUser { Id = 17, Nome = "Analista" };
        var requirement = new Requisito { IdRequisito = 23, IdUsuario = owner.Id, Usuario = owner };
        var operation = ValidModel();
        operation.IdUsuario = owner.Id;
        operation.Usuario = owner;
        operation.IdRequisito = requirement.IdRequisito;
        operation.Requisito = requirement;
        Assert.Same(owner, operation.Usuario);
        Assert.Same(requirement, operation.Requisito);
        Assert.Equal(owner.Id, operation.IdUsuario);
        Assert.Equal(requirement.IdRequisito, operation.IdRequisito);
        Assert.Equal(1L, requirement.VersaoTextual);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(long.MaxValue, true)]
    public void TextVersionMustBePositive(long version, bool valid)
    {
        var operation = ValidModel();
        operation.VersaoTextual = version;
        Assert.Equal(valid, Validate(operation).Count == 0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sha256")]
    public void FingerprintIsMandatoryAndRejectsInvalidText(string? fingerprint)
    {
        var operation = ValidModel();
        operation.Fingerprint = fingerprint!;
        Assert.Contains(Validate(operation), error => error.MemberNames.Contains(nameof(OperacaoAnalise.Fingerprint)));
    }

    [Theory]
    [InlineData('a', 63, false)]
    [InlineData('a', 64, true)]
    [InlineData('F', 64, true)]
    [InlineData('0', 64, true)]
    [InlineData('a', 65, false)]
    [InlineData('g', 64, false)]
    public void FingerprintRequiresExactly64HexadecimalCharacters(char character, int length, bool valid)
    {
        var operation = ValidModel();
        var fingerprint = new string(character, length);
        operation.Fingerprint = fingerprint;
        Assert.Equal(valid, Validate(operation).Count == 0);
        Assert.Equal(fingerprint, operation.Fingerprint);
    }

    [Fact]
    public void FingerprintAcceptsMixedCaseWithoutNormalizingOrIgnoringTrailingNewlines()
    {
        var operation = ValidModel();
        var mixed = string.Concat(Enumerable.Repeat("aB09", 16));
        operation.Fingerprint = mixed;
        Assert.Empty(Validate(operation));
        Assert.Equal(mixed, operation.Fingerprint);
        operation.Fingerprint += "\n";
        Assert.Contains(Validate(operation), error => error.MemberNames.Contains(nameof(OperacaoAnalise.Fingerprint)));
    }

    [Theory]
    [InlineData("Recebida", true)]
    [InlineData("Enviada", true)]
    [InlineData("ResultadoDisponivel", true)]
    [InlineData("Persistida", true)]
    [InlineData("FalhaEncerrada", true)]
    [InlineData("Incerta", true)]
    [InlineData("ResultadoDisponível", false)]
    [InlineData("recebida", false)]
    [InlineData("Enviada ", false)]
    [InlineData("Cancelada", false)]
    [InlineData("Timeout", false)]
    [InlineData("Reprocessando", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void StateAcceptsOnlyTheSixApprovedIdentifiers(string? state, bool valid)
    {
        var operation = ValidModel();
        operation.Estado = state!;
        Assert.Equal(valid, Validate(operation).Count == 0);
    }

    [Theory]
    [InlineData(nameof(OperacaoAnalise.CriadaEmUtc), DateTimeKind.Utc, true)]
    [InlineData(nameof(OperacaoAnalise.CriadaEmUtc), DateTimeKind.Local, false)]
    [InlineData(nameof(OperacaoAnalise.CriadaEmUtc), DateTimeKind.Unspecified, false)]
    [InlineData(nameof(OperacaoAnalise.EnvioRegistradoEmUtc), DateTimeKind.Utc, true)]
    [InlineData(nameof(OperacaoAnalise.EnvioRegistradoEmUtc), DateTimeKind.Local, false)]
    [InlineData(nameof(OperacaoAnalise.EnvioRegistradoEmUtc), DateTimeKind.Unspecified, false)]
    [InlineData(nameof(OperacaoAnalise.EncerradaEmUtc), DateTimeKind.Utc, true)]
    [InlineData(nameof(OperacaoAnalise.EncerradaEmUtc), DateTimeKind.Local, false)]
    [InlineData(nameof(OperacaoAnalise.EncerradaEmUtc), DateTimeKind.Unspecified, false)]
    [InlineData(nameof(OperacaoAnalise.PrazoExcedidoEmUtc), DateTimeKind.Utc, true)]
    [InlineData(nameof(OperacaoAnalise.PrazoExcedidoEmUtc), DateTimeKind.Local, false)]
    [InlineData(nameof(OperacaoAnalise.PrazoExcedidoEmUtc), DateTimeKind.Unspecified, false)]
    public void PresentDatesRequireUtcWithoutConversion(string field, DateTimeKind kind, bool valid)
    {
        var operation = ValidModel();
        var date = new DateTime(2026, 10, 18, 12, 0, 0, kind);
        var property = typeof(OperacaoAnalise).GetProperty(field)!;
        property.SetValue(operation, date);
        Assert.Equal(valid, Validate(operation).Count == 0);
        Assert.Equal(kind, ((DateTime)property.GetValue(operation)!).Kind);
    }

    [Fact]
    public void ExceededDeadlineDoesNotProveTerminationOrChangeState()
    {
        var operation = ValidModel();
        operation.PrazoExcedidoEmUtc = operation.CriadaEmUtc.AddSeconds(60);
        Assert.Empty(Validate(operation));
        Assert.False(operation.TerminoComprovado);
        Assert.Null(operation.EncerradaEmUtc);
        Assert.Equal("Recebida", operation.Estado);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(int.MaxValue, true)]
    [InlineData(-1, false)]
    public void KnownTokensCannotBeNegativeAndUnknownTokensRemainNull(int? tokens, bool valid)
    {
        var operation = ValidModel();
        operation.TokensEntrada = tokens;
        operation.TokensSaida = tokens;
        Assert.Equal(valid, Validate(operation).Count == 0);
        Assert.Equal(tokens, operation.TokensEntrada);
        Assert.Equal(tokens, operation.TokensSaida);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void ModelIdentifierIsRequiredAndNonWhitespace(string? model)
    {
        var operation = ValidModel();
        operation.Modelo = model!;
        Assert.Contains(Validate(operation), error => error.MemberNames.Contains(nameof(OperacaoAnalise.Modelo)));
    }

    [Fact]
    public void DefaultsDoNotInventIdentifiersEventsConsumptionOrEvidence()
    {
        var operation = new OperacaoAnalise();
        Assert.Equal(Guid.Empty, operation.IdOperacao);
        Assert.Equal(default, operation.CriadaEmUtc);
        Assert.Null(operation.EnvioRegistradoEmUtc);
        Assert.Null(operation.EncerradaEmUtc);
        Assert.Null(operation.PrazoExcedidoEmUtc);
        Assert.False(operation.TerminoComprovado);
        Assert.Null(operation.MotivoFalha);
        Assert.Null(operation.EvidenciaRecuperacao);
        Assert.Null(operation.TokensEntrada);
        Assert.Null(operation.TokensSaida);
        Assert.Equal(0m, operation.CustoApi);
        Assert.Empty(operation.RowVersion);
    }

    [Fact]
    public void OptionalTechnicalSummariesAndModelHaveNoInventedMaximumLength()
    {
        var operation = ValidModel();
        operation.MotivoFalha = new string('m', 6000);
        operation.EvidenciaRecuperacao = new string('e', 6000);
        operation.Modelo = new string('q', 6000);
        Assert.Empty(Validate(operation));
        Assert.Equal(6000, operation.MotivoFalha.Length);
        Assert.Equal(6000, operation.EvidenciaRecuperacao.Length);
        Assert.Equal(6000, operation.Modelo.Length);
    }

    private static List<ValidationResult> Validate(OperacaoAnalise operation)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(operation, new ValidationContext(operation), errors, validateAllProperties: true);
        return errors;
    }

    private static OperacaoAnalise ValidModel() => new()
    {
        IdOperacao = Guid.Parse("ae46f9f3-e2d1-43ed-9b07-53033c2b4c59"),
        VersaoTextual = 1,
        Fingerprint = new string('a', 64),
        Estado = "Recebida",
        CriadaEmUtc = new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc),
        Modelo = "qwen2.5:3b"
    };
}

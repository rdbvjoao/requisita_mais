using System.ComponentModel.DataAnnotations;
using System.Reflection;
using RequisitaPlus.Web.Models;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class SugestaoModelTests
{
    [Fact]
    public void ApprovedPropertiesHaveExpectedTypesAndOnlyTheAnalysisNavigation()
    {
        var expected = new Dictionary<string, Type>
        {
            [nameof(Sugestao.IdSugestao)] = typeof(int),
            [nameof(Sugestao.IdAnalise)] = typeof(int),
            [nameof(Sugestao.Analise)] = typeof(Analise),
            [nameof(Sugestao.OrdemNoResultado)] = typeof(int),
            [nameof(Sugestao.TipoProblema)] = typeof(string),
            [nameof(Sugestao.Explicacao)] = typeof(string),
            [nameof(Sugestao.TextoSugerido)] = typeof(string),
            [nameof(Sugestao.Decisao)] = typeof(string),
            [nameof(Sugestao.TextoAplicado)] = typeof(string),
            [nameof(Sugestao.DecididaEmUtc)] = typeof(DateTime?),
            [nameof(Sugestao.RowVersion)] = typeof(byte[])
        };
        var properties = typeof(Sugestao).GetProperties();
        Assert.Equal(expected.Count, properties.Length);
        foreach (var property in properties)
            Assert.Equal(expected[property.Name], property.PropertyType);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void OrderIsLimitedToOneThroughFive(int order, bool valid)
    {
        var sugestao = ValidModel();
        sugestao.OrdemNoResultado = order;
        Assert.Equal(valid, Validate(sugestao).Count == 0);
    }

    [Theory]
    [InlineData("Ambiguidade", true)]
    [InlineData("Termo vago", true)]
    [InlineData("Falta de clareza", true)]
    [InlineData("Falta de especificidade", true)]
    [InlineData("ambiguidade", false)]
    [InlineData("Termo  vago", false)]
    [InlineData("Termo vago ", false)]
    [InlineData("Outro", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ProblemTypeAcceptsOnlyExactApprovedLabels(string? type, bool valid)
    {
        var sugestao = ValidModel();
        sugestao.TipoProblema = type!;
        Assert.Equal(valid, Validate(sugestao).Count == 0);
        Assert.Equal(50, typeof(Sugestao).GetProperty(nameof(Sugestao.TipoProblema))!
            .GetCustomAttribute<StringLengthAttribute>()!.MaximumLength);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    [InlineData("\u2003\u00a0")]
    public void ExplanationRejectsNullEmptyAndWhitespace(string? text)
    {
        var sugestao = ValidModel();
        sugestao.Explicacao = text!;
        Assert.Contains(Validate(sugestao), error => error.MemberNames.Contains(nameof(Sugestao.Explicacao)));
    }

    [Theory]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public void ExplanationCountsUtf16WithoutTruncating(int length, bool valid)
    {
        var sugestao = ValidModel();
        var text = new string('á', length - 2) + "😀";
        sugestao.Explicacao = text;
        Assert.Equal(length, text.Length);
        Assert.Equal(valid, Validate(sugestao).Count == 0);
        Assert.Equal(text, sugestao.Explicacao);
    }

    [Fact]
    public void UnicodeExplanationNeedsNoSeparateExampleOrAutomaticRewriting()
    {
        const string text = "  Expressão vaga: revisão — 李 😀.\r\n  ";
        var sugestao = ValidModel();
        sugestao.Explicacao = text;
        Assert.Empty(Validate(sugestao));
        Assert.Equal(text, sugestao.Explicacao);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("  Proposta da IA — 李 😀  ")]
    public void SuggestedTextPreservesOriginalIncludingNullAndWhitespace(string? text)
    {
        var sugestao = ValidModel();
        sugestao.TextoSugerido = text;
        Assert.Empty(Validate(sugestao));
        Assert.Equal(text, sugestao.TextoSugerido);
        Assert.Null(sugestao.TextoAplicado);
    }

    [Fact]
    public void SuggestedTextHasNoInventedDescriptionLengthLimit()
    {
        var sugestao = ValidModel();
        sugestao.TextoSugerido = new string('á', 6000);
        Assert.Empty(Validate(sugestao));
        Assert.Equal(6000, sugestao.TextoSugerido.Length);
    }

    [Theory]
    [InlineData("Pendente", true)]
    [InlineData("Aplicada", true)]
    [InlineData("Ajustada", true)]
    [InlineData("Descartada", true)]
    [InlineData("pendente", false)]
    [InlineData("Aplicada ", false)]
    [InlineData("Outra", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void DecisionFieldHasOnlyTheApprovedLabelsWithoutExecutingATransition(string? decision, bool valid)
    {
        var sugestao = ValidModel();
        sugestao.Decisao = decision!;
        // Tests only the field domain; definitive-decision coherence belongs to future services/CHECKs.
        Assert.Equal(valid, Validate(sugestao).Count == 0);
        Assert.Null(sugestao.DecididaEmUtc);
        Assert.Null(sugestao.TextoAplicado);
    }

    [Theory]
    [InlineData(5000, true)]
    [InlineData(5001, false)]
    public void AppliedTextHasItsOwnUtf16LimitWithoutChangingTheProposal(int length, bool valid)
    {
        var sugestao = ValidModel();
        sugestao.TextoSugerido = "Proposta original";
        var applied = new string('á', length - 2) + "😀";
        sugestao.TextoAplicado = applied;
        Assert.Equal(valid, Validate(sugestao).Count == 0);
        Assert.Equal(applied, sugestao.TextoAplicado);
        Assert.Equal("Proposta original", sugestao.TextoSugerido);
    }

    [Fact]
    public void InitialDecisionHasNoAppliedTextTimestampOrGeneratedRowVersion()
    {
        var sugestao = new Sugestao();
        Assert.Equal("Pendente", sugestao.Decisao);
        Assert.Null(sugestao.TextoSugerido);
        Assert.Null(sugestao.TextoAplicado);
        Assert.Null(sugestao.DecididaEmUtc);
        Assert.Empty(sugestao.RowVersion);
        Assert.Empty(Validate(ValidModel()));
    }

    [Fact]
    public void TextAssignmentsAreIndependentAndDoNotMutateTheLinkedRequirement()
    {
        var requisito = new Requisito { Descricao = "Descrição atual" };
        var analise = new Analise { IdAnalise = 3, Requisito = requisito };
        var sugestao = ValidModel();
        sugestao.IdAnalise = analise.IdAnalise;
        sugestao.Analise = analise;
        sugestao.TextoSugerido = "Proposta da IA";
        sugestao.TextoAplicado = "Redação independente";
        sugestao.TextoSugerido = "Outra proposta em objeto não persistido";

        Assert.Equal("Redação independente", sugestao.TextoAplicado);
        Assert.Equal("Descrição atual", requisito.Descricao);
        Assert.Equal("Pendente", sugestao.Decisao);
        Assert.Null(sugestao.DecididaEmUtc);
        Assert.Same(analise, sugestao.Analise);
        Assert.Equal(analise.IdAnalise, sugestao.IdAnalise);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc, true)]
    [InlineData(DateTimeKind.Local, false)]
    [InlineData(DateTimeKind.Unspecified, false)]
    public void DecisionDateRequiresUtcWhenSuppliedWithoutConversion(DateTimeKind kind, bool valid)
    {
        var sugestao = ValidModel();
        sugestao.DecididaEmUtc = new DateTime(2026, 10, 18, 12, 0, 0, kind);
        Assert.Equal(valid, Validate(sugestao).Count == 0);
        Assert.Equal(kind, sugestao.DecididaEmUtc.Value.Kind);
        Assert.Equal("Pendente", sugestao.Decisao);
    }

    private static List<ValidationResult> Validate(Sugestao sugestao)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(sugestao, new ValidationContext(sugestao), errors, validateAllProperties: true);
        return errors;
    }

    private static Sugestao ValidModel() => new()
    {
        OrdemNoResultado = 1,
        TipoProblema = "Ambiguidade",
        Explicacao = "O termo admite mais de uma interpretação."
    };
}

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RequisitaPlus.Web.Models;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class RequisitoModelTests
{
    [Fact]
    public void ApprovedPropertiesHaveTheExpectedTypesWithoutAdditionalRelationships()
    {
        var expected = new Dictionary<string, Type>
        {
            [nameof(Requisito.IdRequisito)] = typeof(int),
            [nameof(Requisito.IdUsuario)] = typeof(int),
            [nameof(Requisito.Usuario)] = typeof(ApplicationUser),
            [nameof(Requisito.Titulo)] = typeof(string),
            [nameof(Requisito.Descricao)] = typeof(string),
            [nameof(Requisito.Prioridade)] = typeof(string),
            [nameof(Requisito.Status)] = typeof(string),
            [nameof(Requisito.VersaoTextual)] = typeof(long),
            [nameof(Requisito.UltimaOrdemAnalise)] = typeof(long),
            [nameof(Requisito.DataCadastroUtc)] = typeof(DateTime),
            [nameof(Requisito.DataAtualizacaoUtc)] = typeof(DateTime?),
            [nameof(Requisito.RowVersion)] = typeof(byte[])
        };

        var properties = typeof(Requisito).GetProperties();
        Assert.Equal(expected.Count, properties.Length);
        foreach (var property in properties)
            Assert.Equal(expected[property.Name], property.PropertyType);
    }

    [Theory]
    [InlineData(nameof(Requisito.Titulo), 150)]
    [InlineData(nameof(Requisito.Descricao), 5000)]
    public void TextFieldsDeclareRequiredAndLengthLimits(string field, int maximum)
    {
        var property = typeof(Requisito).GetProperty(field)!;
        Assert.NotNull(property.GetCustomAttribute<RequiredAttribute>());
        Assert.Equal(maximum, property.GetCustomAttribute<StringLengthAttribute>()!.MaximumLength);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    [InlineData("\u2003\u00a0")]
    public void RequiredTextRejectsNullEmptyAndWhitespace(string? value)
    {
        var requisito = ValidModel();
        requisito.Titulo = value!;
        requisito.Descricao = value!;
        var errors = Validate(requisito);
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(Requisito.Titulo)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(Requisito.Descricao)));
    }

    [Theory]
    [InlineData(nameof(Requisito.Titulo), 150, true)]
    [InlineData(nameof(Requisito.Titulo), 151, false)]
    [InlineData(nameof(Requisito.Descricao), 5000, true)]
    [InlineData(nameof(Requisito.Descricao), 5001, false)]
    public void TextLengthBoundariesAreValidatedWithoutTruncation(string field, int length, bool valid)
    {
        var requisito = ValidModel();
        var value = new string('á', length);
        var property = typeof(Requisito).GetProperty(field)!;
        property.SetValue(requisito, value);
        Assert.Equal(valid, Validate(requisito).Count == 0);
        Assert.Equal(value, property.GetValue(requisito));
    }

    [Fact]
    public void UnicodeUsesUtf16UnitsAndPreservesAccentsAndSurrogatePairs()
    {
        var requisito = ValidModel();
        requisito.Titulo = new string('á', 148) + "😀";
        requisito.Descricao = "  João — 李 😀  ";
        Assert.Empty(Validate(requisito));
        Assert.Equal(150, requisito.Titulo.Length);
        Assert.Equal("  João — 李 😀  ", requisito.Descricao);
        requisito.Titulo += "á";
        Assert.Contains(Validate(requisito), error => error.MemberNames.Contains(nameof(Requisito.Titulo)));
        Assert.EndsWith("😀á", requisito.Titulo);
    }

    [Theory]
    [InlineData("Alta", true)]
    [InlineData("Média", true)]
    [InlineData("Baixa", true)]
    [InlineData("Media", false)]
    [InlineData("alta", false)]
    [InlineData("Alta ", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void PriorityRequiresOneExactApprovedValue(string? value, bool valid)
    {
        var requisito = ValidModel();
        requisito.Prioridade = value!;
        Assert.Equal(valid, Validate(requisito).Count == 0);
    }

    [Theory]
    [InlineData("Rascunho", true)]
    [InlineData("Analisado", true)]
    [InlineData("Revisado", true)]
    [InlineData("Arquivado", true)]
    [InlineData("rascunho", false)]
    [InlineData("Revisado ", false)]
    [InlineData("Outro", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void StatusRequiresOneExactApprovedValue(string? value, bool valid)
    {
        var requisito = ValidModel();
        requisito.Status = value!;
        Assert.Equal(valid, Validate(requisito).Count == 0);
    }

    [Fact]
    public void ServerControlledPropertiesAreExcludedFromMvcBinding()
    {
        foreach (var field in new[] { "IdRequisito", "IdUsuario", "Usuario", "Status", "VersaoTextual",
                     "UltimaOrdemAnalise", "DataCadastroUtc", "DataAtualizacaoUtc", "RowVersion" })
            Assert.NotNull(typeof(Requisito).GetProperty(field)!.GetCustomAttribute<BindNeverAttribute>());
    }

    [Fact]
    public void DefaultsDoNotSelectPriorityOrGenerateDatesAndRowVersion()
    {
        var requisito = new Requisito();
        Assert.Empty(requisito.Prioridade);
        Assert.Empty(requisito.Status);
        Assert.Equal(default, requisito.DataCadastroUtc);
        Assert.Null(requisito.DataAtualizacaoUtc);
        Assert.Equal(1L, requisito.VersaoTextual);
        Assert.Equal(0L, requisito.UltimaOrdemAnalise);
        Assert.Empty(requisito.RowVersion);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, -1, false)]
    [InlineData(1, 0, true)]
    [InlineData(long.MaxValue, long.MaxValue, true)]
    public void CountersHaveApprovedNumericDomains(long version, long order, bool valid)
    {
        var requisito = ValidModel();
        requisito.VersaoTextual = version;
        requisito.UltimaOrdemAnalise = order;
        Assert.Equal(valid, Validate(requisito).Count == 0);
    }

    [Fact]
    public void RowVersionAndTextualVersionAreIndependentAndOwnerNavigationIsCompatible()
    {
        var user = new ApplicationUser { Id = 17, Nome = "Analista" };
        var requisito = ValidModel();
        requisito.IdUsuario = user.Id;
        requisito.Usuario = user;
        requisito.RowVersion = [1, 2, 3];
        Assert.Equal(1L, requisito.VersaoTextual);
        requisito.VersaoTextual = 2;
        Assert.Equal(new byte[] { 1, 2, 3 }, requisito.RowVersion);
        Assert.Same(user, requisito.Usuario);
        Assert.Equal(user.Id, requisito.IdUsuario);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc, true)]
    [InlineData(DateTimeKind.Local, false)]
    [InlineData(DateTimeKind.Unspecified, false)]
    public void DatesMustRepresentUtcWithoutAutomaticConversion(DateTimeKind kind, bool valid)
    {
        var requisito = ValidModel();
        var date = new DateTime(2026, 10, 18, 12, 0, 0, kind);
        requisito.DataCadastroUtc = date;
        requisito.DataAtualizacaoUtc = date;
        Assert.Equal(valid, Validate(requisito).Count == 0);
        Assert.Equal(kind, requisito.DataCadastroUtc.Kind);
        Assert.Equal(kind, requisito.DataAtualizacaoUtc!.Value.Kind);
    }

    private static List<ValidationResult> Validate(Requisito requisito)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(requisito, new ValidationContext(requisito), errors, validateAllProperties: true);
        return errors;
    }

    private static Requisito ValidModel() => new()
    {
        Titulo = "Requisito válido",
        Descricao = "Descrição válida.",
        Prioridade = "Média",
        Status = "Rascunho",
        DataCadastroUtc = new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc)
    };
}

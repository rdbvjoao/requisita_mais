using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using RequisitaPlus.Web.Models;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class ApplicationUserModelTests
{
    [Fact]
    public void InheritsIdentityWithIntegerKeyCompatibleWithStandardRole()
    {
        Assert.Equal(typeof(IdentityUser<int>), typeof(ApplicationUser).BaseType);
        Assert.Equal(typeof(int), typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.Id))!.PropertyType);
        Assert.Equal(typeof(int), typeof(IdentityRole<int>).GetProperty(nameof(IdentityRole<int>.Id))!.PropertyType);
    }

    [Fact]
    public void NomeDeclaresRequiredStringWithMaximumLengthOf100()
    {
        var property = typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.Nome))!;

        Assert.Equal(typeof(string), property.PropertyType);
        var required = Assert.IsType<RequiredAttribute>(property.GetCustomAttribute<RequiredAttribute>());
        Assert.False(required.AllowEmptyStrings);
        Assert.Equal(100, property.GetCustomAttribute<StringLengthAttribute>()!.MaximumLength);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    [InlineData("\u2003\u00a0")]
    public void DataAnnotationsRejectNullEmptyOrWhitespaceNome(string? nome)
    {
        var user = new ApplicationUser { Nome = nome! };
        var errors = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(user, new ValidationContext(user), errors, validateAllProperties: true));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(ApplicationUser.Nome)));
    }

    [Fact]
    public void UnicodeNameIsPreservedWithoutTrimmingOrTruncation()
    {
        const string nome = "  João Conceição — 李  ";
        var user = new ApplicationUser { Nome = nome };

        Assert.True(Validator.TryValidateObject(user, new ValidationContext(user), [], validateAllProperties: true));
        Assert.Equal(nome, user.Nome);
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void DataAnnotationsEnforceNomeLengthWithoutChangingTheValue(int length, bool expectedValid)
    {
        var nome = new string('á', length);
        var user = new ApplicationUser { Nome = nome };

        Assert.Equal(expectedValid,
            Validator.TryValidateObject(user, new ValidationContext(user), [], validateAllProperties: true));
        Assert.Equal(nome, user.Nome);
    }

    [Theory]
    [InlineData(nameof(ApplicationUser.Id))]
    [InlineData(nameof(ApplicationUser.Email))]
    [InlineData(nameof(ApplicationUser.NormalizedEmail))]
    [InlineData(nameof(ApplicationUser.UserName))]
    [InlineData(nameof(ApplicationUser.NormalizedUserName))]
    [InlineData(nameof(ApplicationUser.PasswordHash))]
    public void IdentityPropertiesRemainInheritedWithoutRedeclaration(string propertyName)
    {
        var property = typeof(ApplicationUser).GetProperty(propertyName)!;

        Assert.Equal(typeof(IdentityUser<int>), property.DeclaringType);
    }

    [Fact]
    public void OnlyNomeIsAddedWithoutCustomIdentifierOrPasswordStorage()
    {
        var declared = typeof(ApplicationUser).GetProperties(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.Collection(declared, property => Assert.Equal(nameof(ApplicationUser.Nome), property.Name));
    }
}

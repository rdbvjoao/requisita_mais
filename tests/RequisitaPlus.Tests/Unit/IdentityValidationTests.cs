using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RequisitaPlus.Web.Areas.Identity.Pages.Account;
using RequisitaPlus.Web.Models;
using RequisitaPlus.Tests.Support;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

public sealed class IdentityValidationTests
{
    internal static bool Valid(RegisterModel.InputModel input) => Validator.TryValidateObject(input, new ValidationContext(input), [], true);
    internal static RegisterModel.InputModel Input() => new() { Nome = "Analista", Email = "analista@example.invalid", Password = "Aa1!aaaa", ConfirmPassword = "Aa1!aaaa" };

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void NameLimitIncludesTrailingSpaces(int length, bool expected)
    {
        var input = Input();
        input.Nome = "N" + new string(' ', length - 1);
        Assert.Equal(expected, Valid(input));
    }

    [Theory]
    [InlineData(150, true)]
    [InlineData(151, false)]
    public void EmailLengthBoundary(int length, bool expected)
    {
        var input = Input();
        var suffix = "@" + new string('b', 60) + "." + new string('c', length - 124);
        input.Email = new string('a', 62) + suffix;
        Assert.Equal(length, input.Email.Length);
        Assert.Equal(expected, Valid(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\u00a0\t")]
    public void RequiredFieldsRejectBlank(string blank)
    {
        foreach (var property in new[] { "Nome", "Email", "Password", "ConfirmPassword" })
        {
            var input = Input();
            typeof(RegisterModel.InputModel).GetProperty(property)!.SetValue(input, blank);
            Assert.False(Valid(input));
        }
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@example.invalid")]
    [InlineData("analista@")]
    [InlineData("a@@example.invalid")]
    [InlineData("a\r\n@example.invalid")]
    public void InvalidEmailRejected(string email)
    {
        var input = Input(); input.Email = email;
        Assert.False(Valid(input));
    }

    [Fact]
    public void ConfirmationAndUtf16LimitsAreValidated()
    {
        var input = Input(); input.ConfirmPassword = "different";
        Assert.False(Valid(input));
        input = Input(); input.Nome = string.Concat(Enumerable.Repeat("😀", 50));
        Assert.True(Valid(input));
        input.Nome += "x";
        Assert.False(Valid(input));
    }

    [Theory]
    [InlineData("Aa1!aaaa", true)]
    [InlineData("Aa1!aaa", false)]
    [InlineData("aa1!aaaa", false)]
    [InlineData("AA1!AAAA", false)]
    [InlineData("AaA!aaaa", false)]
    [InlineData("Aa11aaaa", false)]
    public async Task FrameworkPasswordRulesRemainInForce(string password, bool expected)
    {
        using var services = PilotSecurityTests.Services();
        using var scope = services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Equal(expected, (await new PasswordValidator<ApplicationUser>().ValidateAsync(manager, new ApplicationUser(), password)).Succeeded);
        var input = Input();
        input.Password = input.ConfirmPassword = password;
        Assert.Equal(expected, Valid(input));
    }

    [Fact]
    public async Task FrameworkNormalizesEmailAndRejectsDuplicateAndInvalidUserName()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        Program.ConfigureServices(services, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        services.Replace(ServiceDescriptor.Singleton<IUserStore<ApplicationUser>, SimulatedIdentityStore>());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var first = new ApplicationUser { Nome = "Primeiro", Email = "Analista@example.invalid", UserName = "Analista@example.invalid" };
        Assert.True((await manager.CreateAsync(first, "Aa1!aaaa")).Succeeded);
        Assert.Equal(manager.NormalizeEmail(first.Email), first.NormalizedEmail);
        Assert.Equal(first.NormalizedEmail, first.NormalizedUserName);
        var second = new ApplicationUser { Nome = "Segundo", Email = "ANALISTA@EXAMPLE.INVALID", UserName = "ANALISTA@EXAMPLE.INVALID" };
        var result = await manager.CreateAsync(second, "Aa1!aaaa");
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, x => x.Code == "DuplicateEmail");
        var invalid = new ApplicationUser { Nome = "Inválido", Email = "a b@example.invalid", UserName = "a b@example.invalid" };
        Assert.False((await manager.CreateAsync(invalid, "Aa1!aaaa")).Succeeded);
    }
}

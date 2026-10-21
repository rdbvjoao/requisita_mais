using Microsoft.Extensions.DependencyInjection;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.Services.IA;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

public sealed class PilotOllamaOptionsTests
{
    [Fact]
    public void ApprovedDefaultsAndReductionsAreAccepted()
    {
        var validator = new OllamaOptionsValidator();
        Assert.True(validator.Validate(null, new OllamaOptions()).Succeeded);
        Assert.True(validator.Validate(null, new OllamaOptions
        { ContextTokens = 2048, MaxOutputTokens = 512, OperationTimeoutSeconds = 30, MaxProblems = 3 }).Succeeded);
    }

    [Theory]
    [InlineData("BaseUrl", "http://localhost:11434")]
    [InlineData("BaseUrl", "https://127.0.0.1:11434")]
    [InlineData("BaseUrl", "http://127.0.0.1:11434/")]
    [InlineData("BaseUrl", "http://127.0.0.1:11434/api/generate")]
    [InlineData("BaseUrl", "http://user:secret@127.0.0.1:11434")]
    [InlineData("BaseUrl", "http://127.0.0.1:11434?secret=value")]
    [InlineData("BaseUrl", "http://127.0.0.1:11434#fragment")]
    [InlineData("BaseUrl", "http://external.invalid:11434")]
    [InlineData("BaseUrl", "http://127.0.0.1:11435")]
    [InlineData("BaseUrl", "")]
    [InlineData("Model", "another-model")]
    [InlineData("ContextTokens", "4097")]
    [InlineData("ContextTokens", "0")]
    [InlineData("ContextTokens", "invalid-number")]
    [InlineData("MaxOutputTokens", "1025")]
    [InlineData("MaxOutputTokens", "0")]
    [InlineData("MaxOutputTokens", "-1")]
    [InlineData("OperationTimeoutSeconds", "61")]
    [InlineData("OperationTimeoutSeconds", "0")]
    [InlineData("KeepAlive", "6m")]
    [InlineData("KeepAlive", "-1")]
    [InlineData("MaxProblems", "6")]
    [InlineData("MaxProblems", "0")]
    public void InvalidConfigurationDisablesOnlyAiAndDoesNotExposeValues(string key, string value)
    {
        using var services = PilotSecurityTests.Services(new() { [$"Ollama:{key}"] = value });
        using var scope = services.CreateScope();
        var availability = scope.ServiceProvider.GetRequiredService<OllamaDisponibilidade>();
        Assert.False(availability.TryObter(out var options, out var message));
        Assert.Null(options);
        Assert.Equal("IA desabilitada: configuração local inválida.", message);
        // Resolving EF does not open a connection. This is isolation of DI/configuration only.
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<RequisitaPlus.Web.Models.ApplicationUser>>());
    }

    [Fact]
    public void ConfigurationBindingAcceptsApprovedEnvironmentOverrides()
    {
        using var services = PilotSecurityTests.Services(new()
        { ["Ollama:MaxProblems"] = "2", ["Ollama:OperationTimeoutSeconds"] = "45" });
        Assert.True(services.GetRequiredService<OllamaDisponibilidade>().TryObter(out var options, out var message));
        Assert.Null(message);
        Assert.Equal(2, options!.MaxProblems);
        Assert.Equal(45, options.OperationTimeoutSeconds);
    }

    [Fact]
    public void OutputMustLeaveRoomForInput()
    {
        Assert.True(new OllamaOptionsValidator().Validate(null,
            new OllamaOptions { ContextTokens = 1024, MaxOutputTokens = 1024 }).Failed);
    }
}

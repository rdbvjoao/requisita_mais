using RequisitaPlus.Tests.Support;
using RequisitaPlus.Web.Services.IA;
using Xunit;

namespace RequisitaPlus.Tests.Integration.Ollama;

[Trait("Category", "Ollama")]
public sealed class OllamaContractTests
{
    [OllamaFact]
    public async Task LocalModelAcceptsRawSchemaAndReturnsTerminalStructuredResult()
    {
        IntegrationGate.RequireOllama();
        var options = new OllamaOptions();
        using var handler = new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(options.OperationTimeoutSeconds) };
        var client = new OllamaClient(http);
        var ct = TestContext.Current.CancellationToken;
        Assert.True(await client.CompativelAsync(options, ct));
        var prompt = ContratoIA.Prompt("Consulta de requisitos", "O sistema deve apresentar os requisitos rapidamente.", options.MaxProblems);
        Assert.True(ContratoIA.Cabe(prompt, options));
        var resposta = await client.GerarAsync(prompt, options, ct);
        Assert.True(resposta.TerminoComprovado);
        Assert.NotNull(resposta.Problemas);
        Assert.InRange(resposta.TokensEntrada!.Value, 1, options.ContextTokens - options.MaxOutputTokens);
        Assert.InRange(resposta.TokensSaida!.Value, 1, options.MaxOutputTokens - 1);
    }
}

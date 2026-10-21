using Microsoft.Extensions.Options;

namespace RequisitaPlus.Web.Services.IA;

public sealed class OllamaOptionsValidator : IValidateOptions<OllamaOptions>
{
    public ValidateOptionsResult Validate(string? name, OllamaOptions options)
    {
        var errors = new List<string>();
        // Comparação literal impede normalizações de URI que admitiriam destinos não aprovados.
        if (options.BaseUrl != "http://127.0.0.1:11434") errors.Add("Destino Ollama não aprovado.");
        if (options.Model != "qwen2.5:3b") errors.Add("Modelo não aprovado.");
        if (options.ContextTokens is < 1 or > 4096) errors.Add("Contexto fora dos limites aprovados.");
        if (options.MaxOutputTokens is < 1 or > 1024 || options.MaxOutputTokens >= options.ContextTokens)
            errors.Add("Saída fora dos limites aprovados.");
        if (options.OperationTimeoutSeconds is < 1 or > 60) errors.Add("Prazo funcional fora dos limites aprovados.");
        if (options.KeepAlive != "5m") errors.Add("Retenção do modelo não aprovada.");
        if (options.MaxProblems is < 1 or > 5) errors.Add("Quantidade de problemas fora dos limites aprovados.");
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}

using Microsoft.Extensions.Options;

namespace RequisitaPlus.Web.Services.IA;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";
    public string BaseUrl { get; set; } = "http://127.0.0.1:11434";
    public string Model { get; set; } = "qwen2.5:3b";
    public int ContextTokens { get; set; } = 4096;
    public int MaxOutputTokens { get; set; } = 1024;
    public int OperationTimeoutSeconds { get; set; } = 60;
    // Retenção do modelo; nunca prova de término de inferência.
    public string KeepAlive { get; set; } = "5m";
    public int MaxProblems { get; set; } = 5;
}

/// <summary>Porta de configuração da IA. Não faz HTTP nem verifica a disponibilidade real do modelo.</summary>
public sealed class OllamaDisponibilidade(IOptionsFactory<OllamaOptions> factory)
{
    public bool TryObter(out OllamaOptions? options, out string? mensagem)
    {
        try
        {
            options = factory.Create(Options.DefaultName);
            mensagem = null;
            return true;
        }
        catch (Exception exception) when (exception is OptionsValidationException or InvalidOperationException)
        {
            // Não propagar valores de configuração ou mensagens do binder para logs/UI.
            options = null;
            mensagem = "IA desabilitada: configuração local inválida.";
            return false;
        }
    }
}

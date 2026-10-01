namespace TecFlow.Business.Integrations.WhatsApp;

/// <summary>Endpoint e chave global da Evolution API (Baileys) usada nas sessões WhatsApp.</summary>
public class EvolutionApiOptions
{
    public const string SectionName = "EvolutionApi";

    public string BaseUrl { get; set; } = "http://localhost:8080";

    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    public string WebhookUrl { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BaseUrl)
        && Uri.TryCreate(BaseUrl, UriKind.Absolute, out _);
}

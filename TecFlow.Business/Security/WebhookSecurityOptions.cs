namespace TecFlow.Business.Security;

public class WebhookSecurityOptions
{
    public const string SectionName = "Integrations:Webhook";

    public const string HeaderName = "X-Webhook-Secret";

    public string Secret { get; set; } = string.Empty;
}

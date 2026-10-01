namespace TecFlow.Business.Security;

public class WebhookSecurityOptions
{
    public const string SectionName = "Integrations:Webhook";

    public const string HeaderName = "X-Webhook-Secret";

    public const string TelegramSecretHeaderName = "X-Telegram-Bot-Api-Secret-Token";

    public string Secret { get; set; } = string.Empty;
}

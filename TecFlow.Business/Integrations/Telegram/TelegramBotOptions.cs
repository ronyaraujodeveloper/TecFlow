namespace TecFlow.Business.Integrations.Telegram;

public class TelegramBotOptions
{
    public const string SectionName = "Telegram";

    public string WebhookBaseUrl { get; set; } = "http://localhost:5001";

    public string UserBotSessionFolder { get; set; } = "App_Data/telegram-sessions";

    public string BuildUserWebhookUrl(int userId)
    {
        var baseUrl = string.IsNullOrWhiteSpace(WebhookBaseUrl)
            ? "http://localhost:5001"
            : WebhookBaseUrl.TrimEnd('/');
        return $"{baseUrl}/api/v1/integrations/telegram/webhook/{userId}";
    }
}

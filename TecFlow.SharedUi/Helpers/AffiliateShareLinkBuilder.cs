namespace TecFlow.SharedUi.Helpers;

/// <summary>Monta URIs de compartilhamento WhatsApp/Telegram com URL encoding.</summary>
public static class AffiliateShareLinkBuilder
{
    public const string WhatsAppSendBase = "https://api.whatsapp.com/send";
    public const string TelegramShareBase = "https://t.me/share/url";

    public static string BuildShareIntro(string? platformDetected) =>
        string.IsNullOrWhiteSpace(platformDetected)
            ? "Confira este produto com meu link de comissão TecFlow:"
            : $"Confira este produto na {platformDetected.Trim()} com meu link de comissão:";

    public static string BuildShareMessage(string convertedUrl, string? platformDetected)
    {
        var url = (convertedUrl ?? string.Empty).Trim();
        var intro = BuildShareIntro(platformDetected);
        return string.IsNullOrEmpty(url) ? intro : $"{intro} {url}";
    }

    public static string BuildWhatsAppUri(string convertedUrl, string? platformDetected = null)
    {
        var text = BuildShareMessage(convertedUrl, platformDetected);
        return $"{WhatsAppSendBase}?text={Uri.EscapeDataString(text)}";
    }

    public static string BuildTelegramUri(string convertedUrl, string? platformDetected = null)
    {
        var url = (convertedUrl ?? string.Empty).Trim();
        var text = BuildShareIntro(platformDetected);
        return $"{TelegramShareBase}?url={Uri.EscapeDataString(url)}&text={Uri.EscapeDataString(text)}";
    }
}

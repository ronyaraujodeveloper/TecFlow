namespace Ofertio.Business.Service.Attribution;

public enum ClickChannelOrigin
{
    Unknown,
    Explicit,
    InApp,
    Referer
}

/// <summary>Resultado da classificação de um clique.</summary>
public sealed record ClickClassification(string Channel, ClickChannelOrigin Origin, bool IsBot)
{
    public string OriginName => Origin.ToString();
}

/// <summary>
/// Decide o canal e se o clique é de robô (prévia de link de WhatsApp, Telegram etc.).
/// Prioridade do canal: caminho explícito, depois navegador interno de aplicativo, depois referer, depois "Direto".
/// </summary>
public static class ClickClassifierRules
{
    private static readonly string[] BotMarkers =
    [
        "whatsapp/", "telegrambot", "twitterbot", "facebookexternalhit", "facebot", "slackbot", "slack-imgproxy",
        "discordbot", "linkedinbot", "googlebot", "google-inspectiontool", "bingbot", "bingpreview", "applebot",
        "yandex", "duckduckbot", "baiduspider", "petalbot", "semrushbot", "ahrefsbot", "mj12bot", "dotbot",
        "skypeuripreview", "vkshare", "pinterestbot", "redditbot", "embedly", "quora link preview", "outbrain",
        "bot/", "crawler", "spider", "preview", "headlesschrome", "lighthouse", "pingdom", "uptimerobot",
        "curl/", "wget/", "python-requests", "python-urllib", "go-http-client", "libwww-perl", "java/",
        "node-fetch", "axios/", "postmanruntime", "insomnia"
    ];

    private static readonly (string Marker, string Channel)[] InAppMarkers =
    [
        ("instagram", "Instagram"),
        ("fban", "Facebook"),
        ("fbav", "Facebook"),
        ("fb_iab", "Facebook"),
        ("musical_ly", "TikTok"),
        ("tiktok", "TikTok"),
        ("bytedancewebview", "TikTok"),
        ("telegram", "Telegram")
    ];

    private static readonly (string Marker, string Channel)[] RefererMarkers =
    [
        ("t.me", "Telegram"),
        ("telegram", "Telegram"),
        ("wa.me", "WhatsApp"),
        ("whatsapp", "WhatsApp"),
        ("mail.google", "Email"),
        ("outlook", "Email"),
        ("mail.yahoo", "Email"),
        ("mail.", "Email"),
        ("instagram", "Instagram"),
        ("facebook", "Facebook"),
        ("fb.", "Facebook"),
        ("tiktok", "TikTok"),
        ("twitter", "X"),
        ("x.com", "X"),
        ("t.co", "X"),
        ("google.", "Google")
    ];

    public static bool IsBot(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent) || userAgent.Equals("desconhecido", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var ua = userAgent.ToLowerInvariant();
        return BotMarkers.Any(marker => ua.Contains(marker, StringComparison.Ordinal));
    }

    /// <param name="explicitChannel">Canal que veio no caminho (/w/…) ou na marcação antiga (tf_src); nome ou rótulo.</param>
    public static ClickClassification Classify(string? userAgent, string? referrer, string? explicitChannel)
    {
        var bot = IsBot(userAgent);
        var explicitName = LinkChannelRules.FromName(explicitChannel)?.Name;
        if (explicitName is not null)
        {
            return new ClickClassification(explicitName, ClickChannelOrigin.Explicit, bot);
        }

        if (bot)
        {
            return new ClickClassification(LinkChannelRules.Unknown, ClickChannelOrigin.Unknown, true);
        }

        var ua = (userAgent ?? string.Empty).ToLowerInvariant();
        foreach (var (marker, channel) in InAppMarkers)
        {
            if (ua.Contains(marker, StringComparison.Ordinal))
            {
                return new ClickClassification(channel, ClickChannelOrigin.InApp, false);
            }
        }

        var fromReferer = ChannelFromReferer(referrer);
        if (fromReferer is not null)
        {
            return new ClickClassification(fromReferer, ClickChannelOrigin.Referer, false);
        }

        return new ClickClassification(LinkChannelRules.Direct, ClickChannelOrigin.Unknown, false);
    }

    public static string? ChannelFromReferer(string? referrer)
    {
        if (string.IsNullOrWhiteSpace(referrer))
        {
            return null;
        }

        var normalized = referrer.Trim();
        var known = LinkChannelRules.FromName(normalized);
        if (known is not null)
        {
            return known.Name;
        }

        var host = Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ? uri.Host.ToLowerInvariant() : normalized.ToLowerInvariant();
        foreach (var (marker, channel) in RefererMarkers)
        {
            if (host.Contains(marker, StringComparison.Ordinal))
            {
                return channel;
            }
        }

        return null;
    }
}

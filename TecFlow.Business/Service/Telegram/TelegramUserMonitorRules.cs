using System.Text.RegularExpressions;
using TecFlow.Business.Service.Groups;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.Telegram;

public static class TelegramUserMonitorRules
{
    public const string UserBotSource = "TelegramUserBot";

    public const int HistoryCatchUpPageSize = 100;

    public const int HistoryCatchUpMaxPerChannel = 1500;

    public const int HistoryCatchUpLookbackHours = 48;

    public const string UserBotOfflineSyncMessage =
        "UserBot desconectado. A sessão MTProto não está ativa — autentique ApiId/ApiHash e o PIN em Conexões (Telegram) e sincronize de novo.";

    public const string HttpUrlPattern =
        @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)";

    private static readonly Regex HttpUrlRegex = new(
        HttpUrlPattern,
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly string[] ShortenerHosts =
    [
        "bit.ly",
        "t.me",
        "telegram.me",
        "s.shopee.com.br",
        "s.shopee",
        "tinyurl.com",
        "t.co",
        "cutt.ly",
        "amzn.to",
        "amzn.br",
        "meli.la",
        "mercadolivre.com",
        "shope.ee",
        "magalu.me",
        "magazinevoce.com.br",
        "a.co",
        "link.amazon.com",
        "link.amazon",
        "shp.ee",
        "br.shp.ee",
        "kb.um",
        "kabum.me"
    ];

    private static readonly string[] DealHostTokens =
    [
        "shopee",
        "mercadolivre",
        "mercadolibre",
        "amazon",
        "amzn",
        "magalu",
        "magazineluiza",
        "aliexpress",
        "pelando",
        "promobit",
        "casasbahia",
        "pontofrio",
        "ponto",
        "meli",
        "kabum",
        "americanas",
        "shoptime",
        "extra",
        "netshoes"
    ];

    private static readonly Regex BareCommerceUrlRegex = new(
        @"(?<![/@\w])((?:www\.)?(?:s\.shopee|shopee|mercadolivre|mercadolibre|amazon|amzn|magalu|magazineluiza|aliexpress|pelando|promobit|casasbahia|pontofrio|meli\.la|shope\.ee|magalu\.me|bit\.ly|amzn\.to)[-a-zA-Z0-9()@:%_\+.~#?&/=]*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static IReadOnlyList<string> ExtractHttpUrls(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var urls = new List<string>();
        foreach (Match match in HttpUrlRegex.Matches(text))
        {
            TryAddUrl(urls, match.Value);
        }

        foreach (Match match in BareCommerceUrlRegex.Matches(text))
        {
            var raw = match.Value.Trim().TrimEnd('.', ',', ';', ')', ']', '"', '\'');
            if (!raw.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                raw = "https://" + raw.TrimStart('/');
            }

            TryAddUrl(urls, raw);
        }

        return urls;
    }

    private static void TryAddUrl(List<string> urls, string? candidate)
    {
        var url = candidate?.Trim().TrimEnd('.', ',', ';', ')', ']', '"', '\'');
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || urls.Contains(url, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        urls.Add(url);
    }

    public static bool IsTrackedCommerceUrl(string? url, out MarketplaceType platform)
    {
        platform = default;
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var detected = GroupOfferCaptureRules.DetectPlatform(url);
        if (detected is MarketplaceType.Shopee
            or MarketplaceType.MercadoLivre
            or MarketplaceType.Amazon
            or MarketplaceType.AliExpress
            or MarketplaceType.MagazineLuiza
            or MarketplaceType.CasasBahia
            or MarketplaceType.Kabum)
        {
            platform = detected.Value;
            return true;
        }

        var host = uri.Host.Trim().TrimStart('.').ToLowerInvariant();
        if (ShortenerHosts.Any(item => host == item || host.EndsWith("." + item, StringComparison.Ordinal)))
        {
            return true;
        }

        return DealHostTokens.Any(token => host.Contains(token, StringComparison.Ordinal));
    }

    public static DateTime HistoryCatchUpSinceUtc(DateTime utcNow) =>
        utcNow.AddHours(-HistoryCatchUpLookbackHours);

    public static bool IsWithinCatchUpWindow(DateTime messageUtc, DateTime sinceUtc) =>
        messageUtc == default || messageUtc >= sinceUtc;

    public static string BuildChannelChatId(long channelId) => "-100" + channelId.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public static string ResolveSessionFileName(int userId) => $"user-{userId}.session";

    public const string CodeSentMessage =
        "Código enviado! Verifique as mensagens no seu aplicativo do Telegram e digite o PIN recebido";

    public const string InvalidApiIdMessage =
        "Informe o api_id numérico de my.telegram.org (ex: 28471934), e não o nome App Title";

    public const string ApiIdPlaceholder = "Ex: 28471934";

    public const string ApiIdHelper =
        "Número gerado em my.telegram.org. Não use o ChatID (-100...) nem o Bot Token.";

    public const string ApiIdLooksLikeTokenMessage =
        "Este campo não aceita Bot Token. Cole apenas o api_id numérico de my.telegram.org, sem os dois-pontos.";

    public const string ApiIdLooksLikeChatIdMessage =
        "Este campo não aceita Chat ID. Não cole valores que começam com -100; use o api_id numérico de my.telegram.org.";

    public const string ApiHashPlaceholder = "Ex: a1b2c3d4e5f6...";

    public const string ApiHashHelper = "Hash de 32 caracteres gerado em my.telegram.org.";

    public const string VerificationPinHelper =
        "Digite o PIN numérico de 5 dígitos enviado no seu aplicativo Telegram após clicar em Solicitar Código.";

    public const string ConnectedBadge = "Conectado ✅";

    public const string AwaitingCodeBadge = "Aguardando código";

    public static string SanitizePhoneInput(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var buffer = new System.Text.StringBuilder(16);
        foreach (var ch in raw.Trim())
        {
            if (ch == '+' && buffer.Length == 0)
            {
                buffer.Append('+');
                continue;
            }

            if (char.IsDigit(ch) && buffer.Length < 16)
            {
                buffer.Append(ch);
            }
        }

        if (buffer.Length == 0)
        {
            return string.Empty;
        }

        if (buffer[0] != '+')
        {
            buffer.Insert(0, '+');
        }

        return buffer.Length > 16 ? buffer.ToString(0, 16) : buffer.ToString();
    }

    public static bool TryNormalizeE164Phone(string? raw, out string phone)
    {
        phone = SanitizePhoneInput(raw);
        var digits = phone.StartsWith('+') ? phone[1..] : phone;
        if (digits.Length is < 10 or > 15)
        {
            phone = string.Empty;
            return false;
        }

        phone = "+" + digits;
        return true;
    }

    public static string SanitizePinInput(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        return digits.Length > 5 ? digits[..5] : digits;
    }

    public static bool TryNormalizeVerificationPin(string? raw, out string pin)
    {
        pin = SanitizePinInput(raw);
        return pin.Length == 5;
    }

    public static string SanitizeApiIdInput(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        return new string(raw.Where(char.IsDigit).ToArray());
    }

    public static bool TryParseApiId(string? raw, out int apiId)
    {
        apiId = 0;
        var digits = SanitizeApiIdInput(raw);
        return int.TryParse(digits, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out apiId)
            && apiId > 0;
    }

    public static bool TryValidateApiIdField(string? raw, out int apiId, out string error)
    {
        apiId = 0;
        error = string.Empty;
        var value = (raw ?? string.Empty).Trim();
        if (value.Contains(':'))
        {
            error = ApiIdLooksLikeTokenMessage;
            return false;
        }

        if (value.StartsWith("-100", StringComparison.Ordinal))
        {
            error = ApiIdLooksLikeChatIdMessage;
            return false;
        }

        if (!TryParseApiId(value, out apiId))
        {
            error = InvalidApiIdMessage;
            return false;
        }

        return true;
    }

    public static bool IsFormatError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.Contains("formato", StringComparison.OrdinalIgnoreCase)
            || message.Contains("dois-pontos", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Bot Token", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Chat ID", StringComparison.OrdinalIgnoreCase)
            || message.Contains("-100", StringComparison.Ordinal)
            || message.Contains("App Title", StringComparison.OrdinalIgnoreCase)
            || message.Contains("api_id", StringComparison.OrdinalIgnoreCase)
            || message.Contains("E.164", StringComparison.OrdinalIgnoreCase);
    }
}

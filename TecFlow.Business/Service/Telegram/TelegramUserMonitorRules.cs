using TecFlow.Business.Service.Groups;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.Telegram;

public static class TelegramUserMonitorRules
{
    public const string UserBotSource = "TelegramUserBot";

    public static bool IsTrackedCommerceUrl(string? url, out MarketplaceType platform)
    {
        platform = default;
        var detected = GroupOfferCaptureRules.DetectPlatform(url);
        if (detected is null)
        {
            return false;
        }

        platform = detected.Value;
        return platform is MarketplaceType.Shopee
            or MarketplaceType.MercadoLivre
            or MarketplaceType.Amazon
            or MarketplaceType.AliExpress
            or MarketplaceType.MagazineLuiza;
    }

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

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

    public const string CodeSentMessage = "Código de verificação enviado para o seu aplicativo do Telegram!";

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
}

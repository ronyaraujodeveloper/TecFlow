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
}

using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.Groups;

public static class GroupOfferCaptureRules
{
    public const string WhatsAppChannel = "WhatsApp";
    public const string TelegramChannel = "Telegram";

    public static string? NormalizeChannel(string? channel)
    {
        if (string.IsNullOrWhiteSpace(channel))
        {
            return null;
        }

        if (channel.Contains("telegram", StringComparison.OrdinalIgnoreCase)
            || channel.Equals("tg", StringComparison.OrdinalIgnoreCase))
        {
            return TelegramChannel;
        }

        if (channel.Contains("whats", StringComparison.OrdinalIgnoreCase)
            || channel.Equals("wa", StringComparison.OrdinalIgnoreCase))
        {
            return WhatsAppChannel;
        }

        return null;
    }

    public static bool MatchesChannel(string? value, string? channel)
    {
        var expected = NormalizeChannel(channel);
        if (expected is null)
        {
            return true;
        }

        return string.Equals(NormalizeChannel(value) ?? value, expected, StringComparison.OrdinalIgnoreCase);
    }

    public static string BuildGroupKey(string channel, string groupId) =>
        $"{channel}:{groupId.Trim()}";

    public const int OffersPageSize = 50;

    public static int ResolveLookbackHours(int hours) =>
        hours switch
        {
            10 => 10,
            48 => 48,
            _ => 24
        };

    public static int ResolveOffersSkip(int skip) => Math.Max(0, skip);

    public static int ResolveOffersTake(int take) =>
        take is > 0 and <= 100 ? take : OffersPageSize;

    public static MarketplaceType? DetectPlatform(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (UrlUnshortenerService.TryDetectMarketplace(url, out var platform)
            || UniversalLinkResolverEngine.TryMapDomainToPlatform(url, out platform))
        {
            return platform;
        }

        return null;
    }

    public static decimal? ExtractPrice(string? text, string? url)
    {
        var parsed = ProductMetadataHtmlParser.Parse(
            string.IsNullOrWhiteSpace(text) ? null : $"<html><body>{text}</body></html>",
            url ?? string.Empty);
        return parsed.ProductPrice;
    }

    public static string? ExtractName(string? text, string? url)
    {
        var fromUrl = ProductMetadataHtmlParser.TryExtractMarketplaceProductNameFromUrl(url);
        if (!string.IsNullOrWhiteSpace(fromUrl))
        {
            return fromUrl;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var firstLine = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(line => !line.StartsWith("http", StringComparison.OrdinalIgnoreCase));
        return ProductMetadataHtmlParser.NormalizePersistedProductName(firstLine);
    }
}

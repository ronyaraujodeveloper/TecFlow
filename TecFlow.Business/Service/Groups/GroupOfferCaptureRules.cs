using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.Groups;

public static class GroupOfferCaptureRules
{
    public const string WhatsAppChannel = "WhatsApp";
    public const string TelegramChannel = "Telegram";

    public static string BuildGroupKey(string channel, string groupId) =>
        $"{channel}:{groupId.Trim()}";

    public static int ResolveLookbackHours(int hours) =>
        hours switch
        {
            10 => 10,
            48 => 48,
            _ => 24
        };

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

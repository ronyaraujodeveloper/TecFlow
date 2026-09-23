using System.Text.RegularExpressions;
using TecFlow.Business.Service.LinkStrategies;

namespace TecFlow.Business.Integrations.TikTokShop;

/// <summary>Extrai o productId/itemId de URLs canônicas e encurtadas do TikTok Shop.</summary>
public static class TikTokShopProductUrlParser
{
    public const string UnrecognizedLinkMessage =
        "Não reconhecemos este link do TikTok Shop. Use a URL do produto (shop.tiktok.com) ou um encurtador oficial.";

    public const string HomologProductId = "1729382256910270001";

    private static readonly Regex ProductPathRegex = new(
        @"/(?:view/product|shop/pdp|pdp|product)/(?<productId>[A-Za-z0-9]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex QueryProductRegex = new(
        @"(?:product_id|productId|item_id|itemId)=(?<productId>[A-Za-z0-9]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryParse(string? url, out string productId)
    {
        productId = string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var sanitized = Sanitize(url);
        var pathMatch = ProductPathRegex.Match(sanitized);
        if (pathMatch.Success)
        {
            productId = pathMatch.Groups["productId"].Value;
            return productId.Length > 0;
        }

        var queryMatch = QueryProductRegex.Match(sanitized);
        if (queryMatch.Success)
        {
            productId = queryMatch.Groups["productId"].Value;
            return productId.Length > 0;
        }

        return false;
    }

    public static string ParseOrThrow(string? url, bool allowHomologFallback)
    {
        if (TryParse(url, out var productId))
        {
            return productId;
        }

        if (allowHomologFallback)
        {
            return HomologProductId;
        }

        throw new AffiliateLinkGenerationException(UnrecognizedLinkMessage);
    }

    public static string Sanitize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var trimmed = url.Trim();
        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + trimmed;
        }

        return trimmed;
    }
}

using System.Globalization;
using System.Text.RegularExpressions;
using TecFlow.Business.Service.Groups;

namespace TecFlow.Business.Integrations.Shopee;

public static class ShopeeProductPageRules
{
    public static readonly string[] SoldOutPhrases =
    [
        "Produto esgotado",
        "Este produto está esgotado",
        "Item esgotado",
        "Esgotado no momento",
        "Este produto não está mais disponível",
        "Produto indisponível"
    ];

    private static readonly Regex SoldOutJsonRegex = new(
        @"""(?:is_sold_out|sold_out|is_soldout)""\s*:\s*true|""item_status""\s*:\s*""(?:sold_out|soldout)""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex SoldOutClassRegex = new(
        @"class\s*=\s*[""'][^""']*\b(?:sold[-_]?out|soldout|product-sold-out)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex MetaPriceRegex = new(
        @"<meta[^>]+(?:property|name|itemprop)\s*=\s*[""'](?:product:price:amount|og:price:amount|price)[""'][^>]*content\s*=\s*[""'](?<num>[0-9]+(?:[.,][0-9]+)?)[""']|<meta[^>]+content\s*=\s*[""'](?<num>[0-9]+(?:[.,][0-9]+)?)[""'][^>]*(?:property|name|itemprop)\s*=\s*[""'](?:product:price:amount|og:price:amount|price)[""']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex JsonLdOfferPriceRegex = new(
        @"""(?:lowPrice|price)""\s*:\s*""?(?<num>[0-9]+(?:\.[0-9]{1,2})?)""?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex JsonLdBlockRegex = new(
        @"<script[^>]+application/ld\+json[^>]*>(?<json>.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled);

    public static bool IsShopeeUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
        && (uri.Host.Contains("shopee.", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith("shp.ee", StringComparison.OrdinalIgnoreCase));

    public static bool IsSoldOut(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return false;
        }

        foreach (var phrase in SoldOutPhrases)
        {
            if (html.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return SoldOutJsonRegex.IsMatch(html) || SoldOutClassRegex.IsMatch(html);
    }

    public static decimal? TryExtractActivePrice(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var meta = MetaPriceRegex.Match(html);
        if (meta.Success && Parse(meta.Groups["num"].Value) is { } metaPrice)
        {
            return metaPrice;
        }

        foreach (Match block in JsonLdBlockRegex.Matches(html))
        {
            var json = block.Groups["json"].Value;
            if (!json.Contains("Offer", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var offer = JsonLdOfferPriceRegex.Match(json);
            if (offer.Success && Parse(offer.Groups["num"].Value) is { } offerPrice)
            {
                return offerPrice;
            }
        }

        return null;
    }

    private static decimal? Parse(string raw)
    {
        var normalized = raw.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            && OfferPriceTextRules.IsPlausiblePrice(value)
                ? decimal.Round(value, 2, MidpointRounding.AwayFromZero)
                : null;
    }
}

using System.Text.RegularExpressions;
using Ofertio.Business.Service.Groups;
using Ofertio.Business.Service.LinkStrategies;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Integrations.Amazon;

/// <summary>
/// Preço total à vista da caixa de compra da Amazon (<c>apexPriceToPay</c> / <c>priceToPay</c> / <c>#corePrice_feature_div</c>),
/// ignorando preço por unidade ("R$ 16,60 / unidade"), preço riscado ("De:") e o tamanho mini do bloco unitário.
/// </summary>
public static class AmazonProductPageRules
{
    private const int BuyBoxWindowLength = 6000;

    private static readonly Regex[] BuyBoxAnchors =
    [
        new(@"class\s*=\s*[""'][^""']*\bapexPriceToPay\b", RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"class\s*=\s*[""'][^""']*\bpriceToPay\b", RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"id\s*=\s*[""']corePriceDisplay_desktop_feature_div[""']", RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"id\s*=\s*[""']corePrice_feature_div[""']", RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"id\s*=\s*[""']corePrice_desktop[""']", RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"id\s*=\s*[""']corePrice_mobile_feature_div[""']", RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"id\s*=\s*[""']apex_desktop[""']", RegexOptions.CultureInvariant | RegexOptions.Compiled)
    ];

    private static readonly Regex OffscreenRegex = new(
        @"<span[^>]*class\s*=\s*[""'][^""']*\ba-offscreen\b[^""']*[""'][^>]*>(?<raw>[^<]*)</span>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex WholeFractionRegex = new(
        @"a-price-whole[^>]*>\s*(?<whole>\d{1,3}(?:\.\d{3})+|\d+)(?:\s|<[^>]*>|[,.])*?<span[^>]*a-price-fraction[^>]*>\s*(?<fraction>\d{2})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex StandalonePriceRegex = new(
        @"^\s*R\$\s*(?<num>\d{1,3}(?:\.\d{3})*,\d{2}|\d+,\d{2}|\d+\.\d{2})\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly string[] MetaPriceProperties = ["og:price:amount", "product:price:amount"];

    public static bool IsAmazonUrl(string? url) =>
        GroupOfferCaptureRules.DetectPlatform(url) == MarketplaceType.Amazon;

    /// <summary>Caixa de compra primeiro; sem ela, <c>og:price:amount</c>. Null quando a página não traz nenhum dos dois.</summary>
    public static decimal? TryExtractBuyBoxPrice(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        foreach (var anchor in BuyBoxAnchors)
        {
            var match = anchor.Match(html);
            if (!match.Success)
            {
                continue;
            }

            var window = html.Substring(match.Index, Math.Min(BuyBoxWindowLength, html.Length - match.Index));
            if ((ReadOffscreenPrice(window) ?? ReadWholeFractionPrice(window)) is { } price)
            {
                return price;
            }
        }

        return ReadMetaPrice(html);
    }

    private static decimal? ReadOffscreenPrice(string window)
    {
        foreach (Match match in OffscreenRegex.Matches(window))
        {
            var raw = DecodeSpaces(match.Groups["raw"].Value);
            var standalone = StandalonePriceRegex.Match(raw);
            if (!standalone.Success || IsExcludedPrice(window, match.Index, match.Length))
            {
                continue;
            }

            var price = ProductMetadataHtmlParser.TryParseDisplayPrice(standalone.Groups["num"].Value);
            if (OfferPriceTextRules.IsPlausiblePrice(price))
            {
                return price;
            }
        }

        return null;
    }

    private static decimal? ReadWholeFractionPrice(string window)
    {
        foreach (Match match in WholeFractionRegex.Matches(window))
        {
            if (IsExcludedPrice(window, match.Index, match.Length))
            {
                continue;
            }

            var price = ProductMetadataHtmlParser.TryParseDisplayPrice(
                $"{match.Groups["whole"].Value},{match.Groups["fraction"].Value}");
            if (OfferPriceTextRules.IsPlausiblePrice(price))
            {
                return price;
            }
        }

        return null;
    }

    private static bool IsExcludedPrice(string window, int index, int length)
    {
        if (OfferPriceTextRules.IsUnitPriceContext(window, index, length))
        {
            return true;
        }

        var container = ReadEnclosingPriceTag(window, index);
        return container.Contains("data-a-strike=\"true\"", StringComparison.OrdinalIgnoreCase)
            || container.Contains("data-a-size=\"mini\"", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadEnclosingPriceTag(string window, int index)
    {
        var start = window.LastIndexOf("class=\"a-price", index, StringComparison.Ordinal);
        while (start >= 0 && window.AsSpan(start).StartsWith("class=\"a-price-"))
        {
            start = start == 0 ? -1 : window.LastIndexOf("class=\"a-price", start - 1, StringComparison.Ordinal);
        }

        if (start < 0)
        {
            return string.Empty;
        }

        var end = window.IndexOf('>', start);
        return end < 0 ? window[start..] : window[start..end];
    }

    private static decimal? ReadMetaPrice(string html)
    {
        foreach (var property in MetaPriceProperties)
        {
            var match = Regex.Match(
                html,
                $@"<meta[^>]+(?:property|name)\s*=\s*[""']{Regex.Escape(property)}[""'][^>]*content\s*=\s*[""'](?<value>[^""']+)[""']|<meta[^>]+content\s*=\s*[""'](?<value>[^""']+)[""'][^>]*(?:property|name)\s*=\s*[""']{Regex.Escape(property)}[""']",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                continue;
            }

            var price = ProductMetadataHtmlParser.TryParseDisplayPrice(match.Groups["value"].Value);
            if (OfferPriceTextRules.IsPlausiblePrice(price))
            {
                return price;
            }
        }

        return null;
    }

    private static string DecodeSpaces(string value) =>
        value.Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("&#160;", " ", StringComparison.Ordinal)
            .Replace('\u00A0', ' ');
}

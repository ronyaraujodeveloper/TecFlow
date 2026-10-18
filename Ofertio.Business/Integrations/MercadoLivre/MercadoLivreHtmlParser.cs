using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Ofertio.Business.Dto;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Integrations.MercadoLivre;

/// <summary>Extrai todos os cards da vitrine pública lista.mercadolivre.com.br (fallback HTML).</summary>
public static class MercadoLivreHtmlParser
{
    public const int MaxResults = 48;

    private static readonly string[] CardMarkers =
    [
        "ui-search-layout__item",
        "ui-search-result__content",
        "ui-search-result__wrapper",
        "poly-card"
    ];

    private static readonly Regex PermalinkJsonRegex = new(
        @"""permalink""\s*:\s*""(?<url>https:[^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex TitleJsonRegex = new(
        @"""(?:title|name)""\s*:\s*""(?<title>[^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex HeadingRegex = new(
        @"<h2\b[^>]*>(?<title>.*?)</h2>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex TitleAttrRegex = new(
        @"title\s*=\s*[""'](?<title>[^""']+)[""']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex HrefRegex = new(
        @"href\s*=\s*[""'](?<url>[^""']+)[""']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex AnchorRegex = new(
        @"<a\b[^>]*href\s*=\s*[""'](?<url>https?://(?:www\.|produto\.)?mercadoli(?:vre|bre)\.com(?:\.br)?/[^""']+|/+[^""']+)[""'][^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ImgRegex = new(
        @"<img\b[^>]*(?:data-src|src)\s*=\s*[""'](?<img>[^""']+)[""']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex PriceFractionRegex = new(
        @"andes-money-amount__fraction[^>]*>(?<price>[\d\.\,]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex PriceJsonRegex = new(
        @"""(?:price|amount)""\s*:\s*(?<price>\d+(?:\.\d+)?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);

    public static IReadOnlyList<OfficialCatalogProductDto> ParseSearchResults(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        var decoded = WebUtility.HtmlDecode(html);
        var items = new List<OfficialCatalogProductDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        TryAddFromHtmlDocument(items, seen, decoded);
        if (items.Count >= MaxResults)
        {
            return items;
        }

        foreach (var card in SplitLayoutCards(decoded))
        {
            if (!TryAddFromCard(items, seen, card) || items.Count < MaxResults)
            {
                continue;
            }

            return items;
        }

        foreach (Match match in PermalinkJsonRegex.Matches(decoded))
        {
            var url = UnescapeJsonUrl(match.Groups["url"].Value);
            var window = Slice(decoded, match.Index, match.Length, 900);
            var title = ReadGroup(TitleJsonRegex, window, "title");
            if (!TryAdd(items, seen, url, title, TryReadJsonPrice(window), ReadImage(window))
                || items.Count < MaxResults)
            {
                continue;
            }

            return items;
        }

        foreach (Match match in AnchorRegex.Matches(decoded))
        {
            var title = FirstNonEmpty(
                ReadGroup(TitleAttrRegex, match.Value, "title"),
                StripTags(ReadGroup(HeadingRegex, Slice(decoded, match.Index, match.Length, 400), "title")));
            if (!TryAdd(items, seen, match.Groups["url"].Value, title, TryReadFractionPrice(Slice(decoded, match.Index, match.Length, 800)), ReadImage(Slice(decoded, match.Index, match.Length, 800)))
                || items.Count < MaxResults)
            {
                continue;
            }

            return items;
        }

        return items;
    }

    internal static IReadOnlyList<string> SplitLayoutCards(string html)
    {
        var starts = new List<int>();
        foreach (var marker in CardMarkers)
        {
            var index = 0;
            while ((index = html.IndexOf(marker, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                starts.Add(index);
                index += marker.Length;
            }
        }

        starts.Sort();
        var unique = new List<int>();
        foreach (var start in starts)
        {
            if (unique.Count == 0 || start - unique[^1] > 80)
            {
                unique.Add(start);
            }
        }

        var cards = new List<string>(unique.Count);
        for (var i = 0; i < unique.Count; i++)
        {
            var from = unique[i];
            var to = i + 1 < unique.Count ? unique[i + 1] : Math.Min(html.Length, from + 8000);
            cards.Add(html[from..to]);
        }

        return cards;
    }

    private static void TryAddFromHtmlDocument(
        List<OfficialCatalogProductDto> items,
        HashSet<string> seen,
        string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var nodes = SelectVitrineNodes(doc);
        if (nodes.Count == 0)
        {
            return;
        }

        foreach (var node in nodes)
        {
            if (items.Count >= MaxResults)
            {
                return;
            }

            var titleNode = node.SelectSingleNode(".//h2")
                ?? node.SelectSingleNode(".//h3")
                ?? node.SelectSingleNode(".//*[contains(@class, 'poly-component__title')]")
                ?? node.SelectSingleNode(".//a[contains(@class, 'title')]");
            var linkNode = node.SelectSingleNode(".//a[contains(@class, 'poly-component__title')]")
                ?? node.SelectSingleNode(".//a[contains(@class, 'ui-search-link')]")
                ?? node.SelectSingleNode(".//a");
            var imgNode = node.SelectSingleNode(".//img");
            if (titleNode is null || linkNode is null)
            {
                continue;
            }

            var title = FirstNonEmpty(
                StripTags(titleNode.InnerText),
                titleNode.GetAttributeValue("title", null),
                linkNode.GetAttributeValue("title", null));
            TryAdd(
                items,
                seen,
                linkNode.GetAttributeValue("href", string.Empty),
                title,
                ReadCurrentPrice(node),
                ReadNodeImage(imgNode),
                ReadOriginalPrice(node));
        }
    }

    private static decimal? ReadCurrentPrice(HtmlNode card)
    {
        var fraction = card.SelectSingleNode(".//*[contains(@class, 'poly-price__current')]//span[contains(@class, 'andes-money-amount__fraction')]")
            ?? card.SelectNodes(".//span[contains(@class, 'andes-money-amount__fraction')]")
                ?.FirstOrDefault(item => !IsPreviousAmount(item));
        return ReadAmount(fraction);
    }

    private static decimal? ReadOriginalPrice(HtmlNode card)
    {
        var fraction = card.SelectNodes(".//span[contains(@class, 'andes-money-amount__fraction')]")
            ?.FirstOrDefault(IsPreviousAmount);
        return ReadAmount(fraction);
    }

    private static bool IsPreviousAmount(HtmlNode fraction) =>
        fraction.Ancestors().Any(ancestor =>
            ancestor.Name.Equals("s", StringComparison.OrdinalIgnoreCase)
            || ancestor.GetAttributeValue("class", string.Empty).Contains("andes-money-amount--previous", StringComparison.OrdinalIgnoreCase));

    private static decimal? ReadAmount(HtmlNode? fraction)
    {
        var whole = ParsePriceFraction(fraction?.InnerText);
        if (whole is null)
        {
            return null;
        }

        var cents = fraction!.ParentNode?.SelectSingleNode(".//span[contains(@class, 'andes-money-amount__cents')]")?.InnerText;
        return int.TryParse(cents?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value is > 0 and < 100
            ? whole + value / 100m
            : whole;
    }

    private static string? ReadNodeImage(HtmlNode? img)
    {
        if (img is null)
        {
            return null;
        }

        var src = img.GetAttributeValue("src", null);
        return string.IsNullOrWhiteSpace(src) || src.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            ? FirstNonEmpty(img.GetAttributeValue("data-src", null), src)
            : src;
    }

    private static List<HtmlNode> SelectVitrineNodes(HtmlDocument doc)
    {
        var nodes = new List<HtmlNode>();
        var seen = new HashSet<HtmlNode>();
        AppendNodes(seen, nodes, doc.DocumentNode.SelectNodes("//li[contains(@class, 'ui-search-layout__item')]"));
        AppendNodes(seen, nodes, doc.DocumentNode.SelectNodes("//*[contains(@class, 'poly-card')]"));
        AppendNodes(seen, nodes, doc.DocumentNode.SelectNodes("//div[contains(@class, 'ui-search-result')]"));
        return nodes;
    }

    private static void AppendNodes(HashSet<HtmlNode> seen, List<HtmlNode> target, HtmlNodeCollection? source)
    {
        if (source is null)
        {
            return;
        }

        foreach (var node in source)
        {
            if (seen.Add(node))
            {
                target.Add(node);
            }
        }
    }

    private static bool TryAddFromCard(
        List<OfficialCatalogProductDto> items,
        HashSet<string> seen,
        string card)
    {
        var hrefMatch = HrefRegex.Match(card);
        if (!hrefMatch.Success)
        {
            return false;
        }

        var title = FirstNonEmpty(
            StripTags(ReadGroup(HeadingRegex, card, "title")),
            ReadGroup(TitleAttrRegex, card, "title"));
        var price = TryReadFractionPrice(card) ?? TryReadJsonPrice(card);
        return TryAdd(items, seen, hrefMatch.Groups["url"].Value, title, price, ReadImage(card));
    }

    private static bool TryAdd(
        List<OfficialCatalogProductDto> items,
        HashSet<string> seen,
        string rawUrl,
        string? title,
        decimal? price,
        string? image,
        decimal? originalPrice = null)
    {
        var url = NormalizeProductUrl(rawUrl);
        if (string.IsNullOrWhiteSpace(url)
            || string.IsNullOrWhiteSpace(title)
            || !IsMercadoLivreProductUrl(url)
            || !seen.Add(CanonicalKey(url)))
        {
            return false;
        }

        MercadoLivreProductUrlParser.TryParse(url, out var itemId);
        items.Add(new OfficialCatalogProductDto
        {
            Platform = "Mercado Livre",
            PlatformType = MarketplaceType.MercadoLivre,
            ProductId = string.IsNullOrWhiteSpace(itemId) ? null : itemId,
            ProductName = title.Trim(),
            Price = price,
            OriginalPrice = originalPrice is not null && originalPrice > price ? originalPrice : null,
            ImageUrl = NormalizeImageUrl(image),
            SourceUrl = url,
            Source = "Web"
        });
        return true;
    }

    private static bool IsMercadoLivreProductUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host;
        if (!host.Contains("mercadolivre", StringComparison.OrdinalIgnoreCase)
            && !host.Contains("mercadolibre", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = uri.AbsolutePath ?? string.Empty;
        return path.Contains("MLB", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/p/", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/_JM", StringComparison.OrdinalIgnoreCase)
            || path.Count(ch => ch == '/') >= 2;
    }

    private static string NormalizeProductUrl(string? raw)
    {
        var url = UnescapeJsonUrl(raw);
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        if (url.StartsWith("//", StringComparison.Ordinal))
        {
            url = "https:" + url;
        }
        else if (url.StartsWith('/'))
        {
            url = "https://www.mercadolivre.com.br" + url;
        }

        var hash = url.IndexOf('#', StringComparison.Ordinal);
        if (hash >= 0)
        {
            url = url[..hash];
        }

        return url.Trim();
    }

    private static string CanonicalKey(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url;
        }

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/').ToLowerInvariant();
    }

    private static string? NormalizeImageUrl(string? image)
    {
        var url = UnescapeJsonUrl(image);
        if (string.IsNullOrWhiteSpace(url) || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (url.StartsWith("//", StringComparison.Ordinal))
        {
            url = "https:" + url;
        }

        return url.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
    }

    private static string UnescapeJsonUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return WebUtility.HtmlDecode(value)
            .Replace("\\/", "/", StringComparison.Ordinal)
            .Replace("\\u0026", "&", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static string Slice(string html, int index, int length, int extra)
    {
        var start = Math.Max(0, index - 80);
        var end = Math.Min(html.Length, index + length + extra);
        return html[start..end];
    }

    private static string? ReadGroup(Regex regex, string text, string group)
    {
        var match = regex.Match(text);
        return match.Success ? match.Groups[group].Value : null;
    }

    private static string? ReadImage(string card)
    {
        var match = ImgRegex.Match(card);
        return match.Success ? match.Groups["img"].Value : null;
    }

    private static decimal? TryReadFractionPrice(string card)
    {
        var match = PriceFractionRegex.Match(card);
        if (!match.Success)
        {
            return null;
        }

        return ParsePriceFraction(match.Groups["price"].Value);
    }

    private static decimal? ParsePriceFraction(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var value = raw.Trim();
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out var ptBr))
        {
            return ptBr;
        }

        var digits = value.Replace(".", string.Empty, StringComparison.Ordinal);
        if (decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out var compact))
        {
            return compact;
        }

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant)
            ? invariant
            : null;
    }

    private static decimal? TryReadJsonPrice(string window)
    {
        var match = PriceJsonRegex.Match(window);
        if (!match.Success)
        {
            return null;
        }

        return decimal.TryParse(
            match.Groups["price"].Value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var price)
            ? price
            : null;
    }

    private static string? StripTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = HtmlTagRegex.Replace(value, " ");
        return WebUtility.HtmlDecode(text).Trim();
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}

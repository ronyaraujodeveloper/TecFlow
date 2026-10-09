using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using TecFlow.Business.Dto;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Integrations.MercadoLivre;

/// <summary>Extrai cards da vitrine pública lista.mercadolivre.com.br (fallback HTML).</summary>
public static class MercadoLivreHtmlParser
{
    public const int MaxResults = 20;

    private static readonly Regex PermalinkJsonRegex = new(
        @"""permalink""\s*:\s*""(?<url>https:[^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex AnchorRegex = new(
        @"<a\b[^>]*href\s*=\s*[""'](?<url>https?://(?:www\.|produto\.)?mercadoli(?:vre|bre)\.com(?:\.br)?/[^""']+)[""'][^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex TitleAttrRegex = new(
        @"title\s*=\s*[""'](?<title>[^""']+)[""']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex TitleJsonRegex = new(
        @"""(?:title|name)""\s*:\s*""(?<title>[^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex PriceJsonRegex = new(
        @"""(?:price|amount)""\s*:\s*(?<price>\d+(?:\.\d+)?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ImageJsonRegex = new(
        @"""(?:thumbnail|secure_thumbnail|image)""\s*:\s*""(?<img>https:[^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static IReadOnlyList<OfficialCatalogProductDto> ParseSearchResults(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        var decoded = WebUtility.HtmlDecode(html);
        var items = new List<OfficialCatalogProductDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in PermalinkJsonRegex.Matches(decoded))
        {
            var url = UnescapeJsonUrl(match.Groups["url"].Value);
            if (!TryAdd(items, seen, url, decoded, match.Index, match.Length))
            {
                continue;
            }

            if (items.Count >= MaxResults)
            {
                return items;
            }
        }

        foreach (Match match in AnchorRegex.Matches(decoded))
        {
            var url = match.Groups["url"].Value;
            var titleMatch = TitleAttrRegex.Match(match.Value);
            var title = titleMatch.Success ? WebUtility.HtmlDecode(titleMatch.Groups["title"].Value) : null;
            if (!TryAdd(items, seen, url, decoded, match.Index, match.Length, title))
            {
                continue;
            }

            if (items.Count >= MaxResults)
            {
                break;
            }
        }

        return items;
    }

    private static bool TryAdd(
        List<OfficialCatalogProductDto> items,
        HashSet<string> seen,
        string rawUrl,
        string html,
        int matchIndex,
        int matchLength,
        string? titleHint = null)
    {
        var url = NormalizeProductUrl(rawUrl);
        if (string.IsNullOrWhiteSpace(url) || !seen.Add(url))
        {
            return false;
        }

        if (!MercadoLivreProductUrlParser.TryParse(url, out var itemId)
            && !url.Contains("MLB", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var window = Slice(html, matchIndex, matchLength, 900);
        var title = FirstNonEmpty(titleHint, ReadGroup(TitleJsonRegex, window, "title"), itemId);
        var image = UnescapeJsonUrl(ReadGroup(ImageJsonRegex, window, "img"));
        items.Add(new OfficialCatalogProductDto
        {
            Platform = "Mercado Livre",
            PlatformType = MarketplaceType.MercadoLivre,
            ProductId = string.IsNullOrWhiteSpace(itemId) ? null : itemId,
            ProductName = title,
            Price = TryReadPrice(window),
            ImageUrl = string.IsNullOrWhiteSpace(image) ? null : image.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase),
            SourceUrl = url,
            Source = "Web"
        });
        return true;
    }

    private static string NormalizeProductUrl(string? raw)
    {
        var url = UnescapeJsonUrl(raw);
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var cut = url.IndexOfAny(['#', ' ']);
        if (cut >= 0)
        {
            url = url[..cut];
        }

        return url.Trim();
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

    private static decimal? TryReadPrice(string window)
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

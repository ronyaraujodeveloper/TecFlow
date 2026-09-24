using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using TecFlow.Business.Dto;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>Parse de HTML de marketplace (OpenGraph, itemprop e JSON-LD) sem I/O.</summary>
public static class ProductMetadataHtmlParser
{
    private static readonly Regex MetaTagRegex = new(
        @"<meta\b[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex JsonLdScriptRegex = new(
        @"<script\b[^>]*type\s*=\s*[""']application/ld\+json[""'][^>]*>(?<json>.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex TitleTagRegex = new(
        @"<title\b[^>]*>(?<title>.*?)</title>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex ItemPropNameRegex = new(
        @"itemprop\s*=\s*[""']name[""'][^>]*>(?<text>[^<]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ItemPropPriceRegex = new(
        @"itemprop\s*=\s*[""']price[""'][^>]*(?:content\s*=\s*[""'](?<content>[^""']+)[""']|>(?<text>[^<]+))",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static ProductMetadataDto Parse(string? html, string pageUrl)
    {
        var fallback = FromUrlFallback(pageUrl);
        if (string.IsNullOrWhiteSpace(html))
        {
            return fallback;
        }

        var name = FirstNonEmpty(
            ReadMeta(html, "og:title"),
            ReadMeta(html, "twitter:title"),
            ReadItemPropName(html),
            ReadJsonLdString(html, "name"),
            ReadHtmlTitle(html));

        var priceRaw = FirstNonEmpty(
            ReadMeta(html, "og:price:amount"),
            ReadMeta(html, "product:price:amount"),
            ReadItemPropPrice(html),
            ReadJsonLdPrice(html));

        var image = FirstNonEmpty(
            ReadMeta(html, "og:image"),
            ReadMeta(html, "twitter:image"),
            ReadJsonLdString(html, "image"));

        return new ProductMetadataDto
        {
            ProductName = Truncate(string.IsNullOrWhiteSpace(name) ? fallback.ProductName : CleanText(name), 255),
            ProductPrice = ParsePrice(priceRaw),
            ProductImageUrl = Truncate(CleanText(image), 500)
        };
    }

    public static ProductMetadataDto FromUrlFallback(string? url) =>
        new()
        {
            ProductName = Truncate(BuildSlugFallback(url), 255),
            ProductPrice = null,
            ProductImageUrl = null
        };

    public static string BuildSlugFallback(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "Produto";
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return Truncate(url.Trim(), 255) ?? "Produto";
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = segments.Length - 1; i >= 0; i--)
        {
            var decoded = WebUtility.UrlDecode(segments[i]) ?? segments[i];
            var withoutQuery = decoded.Split('?', 2)[0];
            var slug = withoutQuery
                .Replace('-', ' ')
                .Replace('_', ' ')
                .Trim();

            if (string.IsNullOrWhiteSpace(slug) || slug.Equals("p", StringComparison.OrdinalIgnoreCase)
                || slug.Equals("dp", StringComparison.OrdinalIgnoreCase)
                || slug.Equals("product", StringComparison.OrdinalIgnoreCase)
                || slug.Equals("produto", StringComparison.OrdinalIgnoreCase)
                || slug.Equals("item", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (slug.All(ch => char.IsDigit(ch) || ch is '.' or ','))
            {
                continue;
            }

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(slug.ToLowerInvariant());
        }

        return string.IsNullOrWhiteSpace(uri.Host) ? "Produto" : uri.Host;
    }

    public static string FormatBrl(decimal? price)
    {
        if (price is not decimal value)
        {
            return "—";
        }

        return value.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
    }

    private static string? ReadMeta(string html, string propertyName)
    {
        foreach (Match match in MetaTagRegex.Matches(html))
        {
            var tag = match.Value;
            var property = ReadAttribute(tag, "property") ?? ReadAttribute(tag, "name");
            if (!string.Equals(property, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var content = ReadAttribute(tag, "content");
            if (!string.IsNullOrWhiteSpace(content))
            {
                return content;
            }
        }

        return null;
    }

    private static string? ReadAttribute(string tag, string attributeName)
    {
        var match = Regex.Match(
            tag,
            $@"{Regex.Escape(attributeName)}\s*=\s*[""'](?<value>[^""']*)[""']",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["value"].Value : null;
    }

    private static string? ReadItemPropName(string html)
    {
        var match = ItemPropNameRegex.Match(html);
        return match.Success ? match.Groups["text"].Value : null;
    }

    private static string? ReadItemPropPrice(string html)
    {
        var match = ItemPropPriceRegex.Match(html);
        if (!match.Success)
        {
            return null;
        }

        return FirstNonEmpty(match.Groups["content"].Value, match.Groups["text"].Value);
    }

    private static string? ReadHtmlTitle(string html)
    {
        var match = TitleTagRegex.Match(html);
        return match.Success ? match.Groups["title"].Value : null;
    }

    private static string? ReadJsonLdString(string html, string propertyName)
    {
        foreach (Match script in JsonLdScriptRegex.Matches(html))
        {
            var json = script.Groups["json"].Value;
            if (string.IsNullOrWhiteSpace(json) || !LooksLikeProductJson(json))
            {
                continue;
            }

            var match = Regex.Match(
                json,
                $@"""{Regex.Escape(propertyName)}""\s*:\s*(?:""(?<text>[^""]+)""|\[\s*""(?<text>[^""]+)"")",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                return match.Groups["text"].Value;
            }
        }

        return null;
    }

    private static string? ReadJsonLdPrice(string html)
    {
        foreach (Match script in JsonLdScriptRegex.Matches(html))
        {
            var json = script.Groups["json"].Value;
            if (string.IsNullOrWhiteSpace(json) || !LooksLikeProductJson(json))
            {
                continue;
            }

            var match = Regex.Match(
                json,
                @"""price""\s*:\s*(?:""(?<text>[^""]+)""|(?<num>-?\d+(?:[.,]\d+)?))",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                return FirstNonEmpty(match.Groups["text"].Value, match.Groups["num"].Value);
            }
        }

        return null;
    }

    private static bool LooksLikeProductJson(string json) =>
        json.Contains("Product", StringComparison.OrdinalIgnoreCase)
        || json.Contains("\"offers\"", StringComparison.OrdinalIgnoreCase);

    private static decimal? ParsePrice(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var cleaned = Regex.Replace(raw, @"[^\d,.\-]", string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return null;
        }

        if (cleaned.Contains(',') && cleaned.Contains('.'))
        {
            if (cleaned.LastIndexOf(',') > cleaned.LastIndexOf('.'))
            {
                cleaned = cleaned.Replace(".", string.Empty).Replace(',', '.');
            }
            else
            {
                cleaned = cleaned.Replace(",", string.Empty);
            }
        }
        else if (cleaned.Contains(','))
        {
            cleaned = cleaned.Replace(',', '.');
        }

        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? decimal.Round(value, 2, MidpointRounding.AwayFromZero)
            : null;
    }

    private static string? CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var decoded = WebUtility.HtmlDecode(value).Trim();
        decoded = Regex.Replace(decoded, @"\s+", " ");
        return string.IsNullOrWhiteSpace(decoded) ? null : decoded;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
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

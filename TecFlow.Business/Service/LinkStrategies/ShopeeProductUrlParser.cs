using System.Text.RegularExpressions;
using TecFlow.Business.Integrations.Shopee;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>Identificadores de produto extraídos de uma URL canônica Shopee.</summary>
public readonly record struct ShopeeProductUrlIds(string ShopId, string ItemId);

/// <summary>
/// Extrai ShopId e ItemId de URLs Shopee (path -i.shop.item, /product|/item e querystring).
/// </summary>
public static class ShopeeProductUrlParser
{
    public const string HomologShopId = "999999";
    public const string HomologItemId = "888888";

    public const string UnrecognizedLinkMessage =
        "Formato de link da Shopee não reconhecido. Use o link do app ou a URL padrão do produto.";

    public static ShopeeProductUrlIds HomologIds { get; } = new(HomologShopId, HomologItemId);

    /// <summary>Parâmetros de tracking/UI da Shopee que atrapalham o parse e a API de afiliados.</summary>
    private static readonly HashSet<string> NoisyQueryKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "extraparams",
        "sp_atk",
        "xptdk",
        "smtt",
        "uls_trackid",
        "fromsource",
        "is_from_external",
        "sharechannel",
        "publish",
        "utm_source",
        "utm_medium",
        "utm_campaign",
        "utm_content",
        "utm_term",
        "gclid",
        "fbclid"
    };

    private static readonly Regex HyphenItemPattern = new(
        @"[-_/]i\.(\d+)\.(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ProductPathPattern = new(
        @"/(?:universal-link/)?(?:product|item)/(\d+)/(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ShopItemPathPattern = new(
        @"/shop/(\d+)/(?:item|product)/(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TrailingNumericPairPattern = new(
        @"\.(\d{4,})\.(\d{4,})(?:/|$|\?|#)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] NestedUrlQueryKeys = ["url", "redir", "redirect", "target", "u", "link"];

    public static bool TryParse(string? url, out ShopeeProductUrlIds ids) =>
        TryParseInternal(Sanitize(url), out ids, depth: 0)
        || TryParseInternal(url, out ids, depth: 0)
        || TryParseHyphenInRaw(url, out ids);

    public static ShopeeProductUrlIds ParseOrThrow(string url) =>
        ParseOrThrow(url, originalUrl: null, allowHomologFallback: false);

    public static ShopeeProductUrlIds ParseOrThrow(string url, string? originalUrl, bool allowHomologFallback)
    {
        if (TryParse(url, out var ids) || TryParse(originalUrl, out ids))
        {
            return ids;
        }

        if (allowHomologFallback)
        {
            return HomologIds;
        }

        throw new AffiliateLinkGenerationException(UnrecognizedLinkMessage);
    }

    /// <summary>
    /// Remove query params ruidosos (extraParams, sp_atk, xptdk, etc.) e devolve a URL canônica do produto.
    /// </summary>
    public static string Sanitize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var trimmed = url.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return StripQueryFallback(trimmed);
        }

        if (TryParseHyphenInRaw(uri.AbsolutePath, out var hyphenIds)
            || TryParsePathPatterns(uri.AbsolutePath, out hyphenIds))
        {
            return BuildCanonicalProductUrl(uri, hyphenIds);
        }

        var kept = FilterQuery(uri.Query);
        var builder = new UriBuilder(uri)
        {
            Query = kept,
            Fragment = string.Empty
        };

        return builder.Uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped);
    }

    public static string? TryExtractShortHash(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return null;
        }

        var segment = uri.AbsolutePath.Trim('/');
        if (string.IsNullOrWhiteSpace(segment))
        {
            return null;
        }

        var first = segment.Split('/', StringSplitOptions.RemoveEmptyEntries)[0];
        return string.IsNullOrWhiteSpace(first) ? null : first;
    }

    private static bool TryParseInternal(string? url, out ShopeeProductUrlIds ids, int depth)
    {
        ids = default;
        if (depth > 3 || string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return TryParseHyphenInRaw(url, out ids);
        }

        if (TryParseQuery(uri.Query, out ids) || TryParseQuery(uri.Fragment.TrimStart('#'), out ids))
        {
            return true;
        }

        var path = uri.AbsolutePath;
        if (TryParsePathPatterns(path, out ids) || TryParseHyphenInRaw(path, out ids))
        {
            return true;
        }

        foreach (var key in NestedUrlQueryKeys)
        {
            var nested = FindQueryValue(uri.Query, key) ?? FindQueryValue(uri.Fragment.TrimStart('#'), key);
            if (string.IsNullOrWhiteSpace(nested))
            {
                continue;
            }

            if (TryParseInternal(nested, out ids, depth + 1))
            {
                return true;
            }
        }

        return TryParseHyphenInRaw(url, out ids);
    }

    private static bool TryParsePathPatterns(string path, out ShopeeProductUrlIds ids)
    {
        ids = default;

        var pathMatch = ProductPathPattern.Match(path);
        if (pathMatch.Success)
        {
            ids = new ShopeeProductUrlIds(pathMatch.Groups[1].Value, pathMatch.Groups[2].Value);
            return true;
        }

        var shopItemMatch = ShopItemPathPattern.Match(path);
        if (shopItemMatch.Success)
        {
            ids = new ShopeeProductUrlIds(shopItemMatch.Groups[1].Value, shopItemMatch.Groups[2].Value);
            return true;
        }

        var hyphenMatch = HyphenItemPattern.Match(path);
        if (hyphenMatch.Success)
        {
            ids = new ShopeeProductUrlIds(hyphenMatch.Groups[1].Value, hyphenMatch.Groups[2].Value);
            return true;
        }

        var trailingMatch = TrailingNumericPairPattern.Match(path);
        if (trailingMatch.Success)
        {
            ids = new ShopeeProductUrlIds(trailingMatch.Groups[1].Value, trailingMatch.Groups[2].Value);
            return true;
        }

        return false;
    }

    private static bool TryParseHyphenInRaw(string? value, out ShopeeProductUrlIds ids)
    {
        ids = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var hyphenMatch = HyphenItemPattern.Match(value);
        if (hyphenMatch.Success)
        {
            ids = new ShopeeProductUrlIds(hyphenMatch.Groups[1].Value, hyphenMatch.Groups[2].Value);
            return true;
        }

        var trailingMatch = TrailingNumericPairPattern.Match(value);
        if (trailingMatch.Success)
        {
            ids = new ShopeeProductUrlIds(trailingMatch.Groups[1].Value, trailingMatch.Groups[2].Value);
            return true;
        }

        return false;
    }

    private static string BuildCanonicalProductUrl(Uri uri, ShopeeProductUrlIds ids)
    {
        var path = uri.AbsolutePath;
        var hyphenMatch = HyphenItemPattern.Match(path);
        if (hyphenMatch.Success)
        {
            var cleanPath = path[..hyphenMatch.Index] + $"-i.{ids.ShopId}.{ids.ItemId}";
            return $"{uri.Scheme}://{uri.Authority}{cleanPath}";
        }

        return ShopeeCommissionUrlBuilder.ToUniversalWebUrl(ids);
    }

    private static string FilterQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        var normalized = query.TrimStart('?');
        var kept = new List<string>();
        foreach (var pair in normalized.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            var key = separator > 0
                ? Uri.UnescapeDataString(pair[..separator])
                : Uri.UnescapeDataString(pair);

            if (NoisyQueryKeys.Contains(key))
            {
                continue;
            }

            if (key.Equals("shopid", StringComparison.OrdinalIgnoreCase)
                || key.Equals("shop_id", StringComparison.OrdinalIgnoreCase)
                || key.Equals("itemid", StringComparison.OrdinalIgnoreCase)
                || key.Equals("item_id", StringComparison.OrdinalIgnoreCase)
                || key.Equals("product_id", StringComparison.OrdinalIgnoreCase))
            {
                kept.Add(pair);
            }
        }

        return string.Join('&', kept);
    }

    private static string StripQueryFallback(string url)
    {
        var cut = url.IndexOfAny(['?', '#']);
        return cut > 0 ? url[..cut] : url;
    }

    private static bool TryParseQuery(string query, out ShopeeProductUrlIds ids)
    {
        ids = default;
        var shopId = FindQueryValue(query, "shopid", "shop_id", "shopId");
        var itemId = FindQueryValue(query, "itemid", "item_id", "itemId", "product_id");
        if (string.IsNullOrWhiteSpace(shopId) || string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        ids = new ShopeeProductUrlIds(shopId, itemId);
        return true;
    }

    private static string? FindQueryValue(string query, params string[] names)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var normalized = query.TrimStart('?', '#', '/');
        var pairs = normalized.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(pair[..separator]);
            if (!names.Any(name => key.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var value = Uri.UnescapeDataString(pair[(separator + 1)..].Replace("+", " ")).Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}

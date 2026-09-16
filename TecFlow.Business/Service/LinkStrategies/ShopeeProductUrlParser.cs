using System.Text.RegularExpressions;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>Identificadores de produto extraídos de uma URL canônica Shopee.</summary>
public readonly record struct ShopeeProductUrlIds(string ShopId, string ItemId);

/// <summary>
/// Extrai ShopId e ItemId de URLs Shopee (path -i.shop.item, /product|/item e querystring).
/// </summary>
public static class ShopeeProductUrlParser
{
    private static readonly Regex HyphenItemPattern = new(
        @"[-_/]i\.(\d+)\.(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ProductPathPattern = new(
        @"/(?:product|item)/(\d+)/(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryParse(string? url, out ShopeeProductUrlIds ids)
    {
        ids = default;
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (TryParseQuery(uri, out ids))
        {
            return true;
        }

        var pathAndQuery = uri.AbsolutePath + uri.Query;

        var pathMatch = ProductPathPattern.Match(pathAndQuery);
        if (pathMatch.Success)
        {
            ids = new ShopeeProductUrlIds(pathMatch.Groups[1].Value, pathMatch.Groups[2].Value);
            return true;
        }

        var hyphenMatch = HyphenItemPattern.Match(pathAndQuery);
        if (hyphenMatch.Success)
        {
            ids = new ShopeeProductUrlIds(hyphenMatch.Groups[1].Value, hyphenMatch.Groups[2].Value);
            return true;
        }

        return false;
    }

    public static ShopeeProductUrlIds ParseOrThrow(string url)
    {
        if (TryParse(url, out var ids))
        {
            return ids;
        }

        throw new AffiliateLinkGenerationException(
            "Não foi possível extrair ItemId e ShopId da URL Shopee expandida.");
    }

    private static bool TryParseQuery(Uri uri, out ShopeeProductUrlIds ids)
    {
        ids = default;
        var shopId = FindQueryValue(uri.Query, "shopid", "shop_id");
        var itemId = FindQueryValue(uri.Query, "itemid", "item_id");
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

        var pairs = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
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

            var value = Uri.UnescapeDataString(pair[(separator + 1)..]).Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}

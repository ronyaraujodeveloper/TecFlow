using System.Text.RegularExpressions;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>Regras de host para URLs nativas e encurtadas da Shopee.</summary>
public static class ShopeeLinkHostMatcher
{
    private static readonly string[] SupportedHosts =
    [
        "shopee.com.br",
        "shopee.com",
        "s.shopee.com.br",
        "s.shopee.com",
        "m.shopee.com.br",
        "br.shp.ee",
        "shp.ee",
        "shope.ee"
    ];

    private static readonly string[] ShortenerHosts =
    [
        "s.shopee.com.br",
        "s.shopee.com",
        "br.shp.ee",
        "shp.ee",
        "shope.ee"
    ];

    private static readonly Regex ShopeeHostRegex = new(
        @"^(?:www\.)?(?:(?:[a-z0-9-]+\.)*(?:shopee\.com(?:\.br)?|shp\.ee|shope\.ee))$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsNativeDeepLink(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme.Equals("shopee", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsShopeeUrl(string? url) =>
        IsNativeDeepLink(url) || MatchesAnyHost(url, SupportedHosts) || MatchesShopeeHostRegex(url);

    public static bool IsShortenerUrl(string? url) => MatchesAnyHost(url, ShortenerHosts);

    private static bool MatchesShopeeHostRegex(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        return ShopeeHostRegex.IsMatch(NormalizeHost(uri.Host));
    }

    private static bool MatchesAnyHost(string? url, IEnumerable<string> hosts)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = NormalizeHost(uri.Host);
        return hosts.Any(supported =>
            host.Equals(supported, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("." + supported, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeHost(string host)
    {
        var normalized = host.TrimEnd('.').ToLowerInvariant();
        return normalized.StartsWith("www.", StringComparison.Ordinal)
            ? normalized[4..]
            : normalized;
    }
}

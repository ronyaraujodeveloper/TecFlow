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
        "shope.ee"
    ];

    private static readonly string[] ShortenerHosts =
    [
        "s.shopee.com.br",
        "s.shopee.com",
        "shope.ee"
    ];

    public static bool IsNativeDeepLink(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme.Equals("shopee", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsShopeeUrl(string? url) =>
        IsNativeDeepLink(url) || MatchesAnyHost(url, SupportedHosts);

    public static bool IsShortenerUrl(string? url) => MatchesAnyHost(url, ShortenerHosts);

    private static bool MatchesAnyHost(string? url, IEnumerable<string> hosts)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.TrimEnd('.').ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
        {
            host = host[4..];
        }

        return hosts.Any(supported =>
            host.Equals(supported, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("." + supported, StringComparison.OrdinalIgnoreCase));
    }
}

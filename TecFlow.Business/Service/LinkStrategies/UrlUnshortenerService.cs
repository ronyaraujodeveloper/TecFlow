using System.Net;
using System.Text.RegularExpressions;
using TecFlow.Business.Integrations;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>
/// Extração de Location/HTML/JS e detecção de marketplace após a descompactação.
/// </summary>
public static class UrlUnshortenerService
{
    public const int MaxHops = UniversalLinkResolverEngine.MaxHops;

    private static readonly Regex MetaRefreshRegex = new(
        @"<meta\s+[^>]*http-equiv\s*=\s*[""']refresh[""'][^>]*content\s*=\s*[""'][^""']*url\s*=\s*([^""';\s]+)[""']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex MetaRefreshSwapRegex = new(
        @"<meta\s+[^>]*content\s*=\s*[""'][^""']*url\s*=\s*([^""';\s]+)[""'][^>]*http-equiv\s*=\s*[""']refresh[""']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex WindowLocationRegex = new(
        @"window\.location(?:\.href)?\s*=\s*['""]([^'""]+)['""]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex LocationHrefRegex = new(
        @"location\.href\s*=\s*['""]([^'""]+)['""]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex LocationReplaceRegex = new(
        @"location\.replace\(\s*['""]([^'""]+)['""]\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsAggregatorUrl(string? url) =>
        UniversalLinkResolverEngine.IsAggregatorUrl(url);

    public static bool IsSupportedMarketplaceUrl(string? url) =>
        TryDetectMarketplace(url, out _);

    public static bool TryDetectMarketplace(string? url, out MarketplaceType platform)
    {
        platform = default;
        if (UniversalLinkResolverEngine.IsAggregatorUrl(url))
        {
            return false;
        }

        return UniversalLinkResolverEngine.TryMapDomainToPlatform(url, out platform)
            && !AffiliateTrackingIdValidator.IsShortenerUrl(url);
    }

    public static string? TryExtractRedirectFromHtml(string? html, string currentUrl)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var raw = FirstMatch(
            html,
            MetaRefreshRegex,
            MetaRefreshSwapRegex,
            WindowLocationRegex,
            LocationHrefRegex,
            LocationReplaceRegex);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var decoded = WebUtility.HtmlDecode(raw.Trim().Trim('"', '\'', ';'));
        if (Uri.TryCreate(decoded, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return absolute.ToString();
        }

        if (Uri.TryCreate(currentUrl, UriKind.Absolute, out var baseUri)
            && Uri.TryCreate(baseUri, decoded, out var relative)
            && (relative.Scheme == Uri.UriSchemeHttp || relative.Scheme == Uri.UriSchemeHttps))
        {
            return relative.ToString();
        }

        return null;
    }

    public static string StripForeignTracking(string? url) =>
        UniversalLinkResolverEngine.StripCommissionAndTracking(url);

    public static string ApplyTenantCredentials(string storeUrl, string? trackingId) =>
        UniversalLinkResolverEngine.ApplyTenantCredentials(storeUrl, trackingId);

    private static string? FirstMatch(string html, params Regex[] patterns)
    {
        foreach (var pattern in patterns)
        {
            var match = pattern.Match(html);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }
}

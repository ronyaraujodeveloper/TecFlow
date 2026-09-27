using System.Net;
using System.Text.RegularExpressions;
using TecFlow.Business.Integrations;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>
/// Extração de Location/HTML/JS e detecção de marketplace após a descompactação.
/// </summary>
public static class UrlUnshortenerService
{
    public const int MaxHops = UniversalLinkResolverEngine.MaxHops;

    public const int MaxResolutionLoops = 5;

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

    /// <summary>
    /// Percorre até 5 saltos até um marketplace suportado (Amazon, Shopee, Magalu, ML, Kabum, TikTok, Casas Bahia).
    /// Cada iteração só faz HTTP se a URL atual ainda não for destino de loja.
    /// </summary>
    public static async Task<string> ResolveToFinalSupportedMarketplaceAsync(
        string inputUrl,
        IUrlExpansionService expansionService,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expansionService);
        return await ResolveToFinalSupportedMarketplaceAsync(
            inputUrl,
            async (url, token) =>
            {
                var next = await expansionService.ExpandUrlAsync(url, token);
                if (string.IsNullOrWhiteSpace(next)
                    || string.Equals(next, url, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return next;
            },
            cancellationToken);
    }

    public static async Task<string> ResolveToFinalSupportedMarketplaceAsync(
        string inputUrl,
        Func<string, CancellationToken, Task<string?>> followNextHopAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(followNextHopAsync);
        if (string.IsNullOrWhiteSpace(inputUrl))
        {
            return inputUrl;
        }

        var currentUrl = AffiliateTrackingIdValidator.EnsureAbsoluteHttpUrl(inputUrl.Trim());
        var iteration = 0;
        while (iteration < MaxResolutionLoops)
        {
            if (IsSupportedMarketplaceUrl(currentUrl))
            {
                return FinalizeMarketplaceUrl(currentUrl);
            }

            var nextUrl = await followNextHopAsync(currentUrl, cancellationToken);
            if (string.IsNullOrWhiteSpace(nextUrl)
                || string.Equals(nextUrl, currentUrl, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            currentUrl = AffiliateTrackingIdValidator.EnsureAbsoluteHttpUrl(nextUrl.Trim());
            iteration++;
        }

        return IsSupportedMarketplaceUrl(currentUrl)
            ? FinalizeMarketplaceUrl(currentUrl)
            : currentUrl;
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

    private static string FinalizeMarketplaceUrl(string url)
    {
        var unwrapped = AffiliateTrackingIdValidator.UnwrapTikTokLoginRedirect(url);
        return StripForeignTracking(unwrapped);
    }
}

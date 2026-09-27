using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using TecFlow.Business.Integrations;
using TecFlow.Business.Integrations.Amazon;
using TecFlow.Business.Integrations.Kabum;
using TecFlow.Business.Integrations.MagazineLuiza;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>
/// Descompactação de agregadores de ofertas (ofertou.ai, promoby.me):
/// extração de Location/HTML/JS e limpeza de tags do criador original.
/// </summary>
public static class UrlUnshortenerService
{
    public const int MaxHops = 5;

    private static readonly string[] AggregatorHosts =
    [
        "ofertou.ai",
        "promoby.me"
    ];

    private static readonly (string Host, MarketplaceType Platform)[] MarketplaceHosts =
    [
        ("kabum.com.br", MarketplaceType.Kabum),
        ("kb.um", MarketplaceType.Kabum),
        ("kabum.me", MarketplaceType.Kabum),
        ("magazineluiza.com.br", MarketplaceType.MagazineLuiza),
        ("magazinevoce.com.br", MarketplaceType.MagazineLuiza),
        ("magalu.com.br", MarketplaceType.MagazineLuiza),
        ("magalu.me", MarketplaceType.MagazineLuiza),
        ("amazon.com.br", MarketplaceType.Amazon),
        ("amazon.com", MarketplaceType.Amazon),
        ("shopee.com.br", MarketplaceType.Shopee),
        ("shopee.com", MarketplaceType.Shopee),
        ("mercadolivre.com.br", MarketplaceType.MercadoLivre),
        ("mercadolivre.com", MarketplaceType.MercadoLivre),
        ("casasbahia.com.br", MarketplaceType.CasasBahia),
        ("cb.com.br", MarketplaceType.CasasBahia),
        ("shop.tiktok.com", MarketplaceType.TikTokShop),
        ("tiktok.com", MarketplaceType.TikTokShop)
    ];

    private static readonly HashSet<string> ForeignTrackingKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "utm_source",
        "utm_medium",
        "utm_campaign",
        "utm_content",
        "utm_term",
        "utm_id",
        "aff_id",
        "affiliate_id",
        "an_id",
        "mmp_pid",
        "sub_id",
        "tag",
        "partner_id",
        "promoter_id",
        "parceiro",
        "afiliado",
        "ref",
        "ref_",
        "click_id",
        "gclid",
        "fbclid"
    };

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
        TryGetHost(url, out var host)
        && AggregatorHosts.Any(item => host == item || host.EndsWith("." + item, StringComparison.Ordinal));

    public static bool IsSupportedMarketplaceUrl(string? url) =>
        TryDetectMarketplace(url, out _);

    public static bool TryDetectMarketplace(string? url, out MarketplaceType platform)
    {
        platform = default;
        if (IsAggregatorUrl(url) || AffiliateTrackingIdValidator.IsShortenerUrl(url))
        {
            return false;
        }
        if (!TryGetHost(url, out var host))
        {
            return false;
        }

        foreach (var entry in MarketplaceHosts)
        {
            if (host == entry.Host || host.EndsWith("." + entry.Host, StringComparison.Ordinal))
            {
                platform = entry.Platform;
                return true;
            }
        }

        return false;
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

    public static string StripForeignTracking(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return url ?? string.Empty;
        }

        if (string.IsNullOrEmpty(uri.Query))
        {
            return uri.GetLeftPart(UriPartial.Path);
        }

        var kept = new List<string>();
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            if (ForeignTrackingKeys.Contains(key))
            {
                continue;
            }

            kept.Add(pair);
        }

        var builder = new UriBuilder(uri)
        {
            Query = kept.Count == 0 ? string.Empty : string.Join('&', kept),
            Fragment = string.Empty
        };
        return builder.Uri.ToString();
    }

    public static string ApplyTenantCredentials(string storeUrl, string? trackingId)
    {
        var cleaned = StripForeignTracking(storeUrl);
        if (!TryDetectMarketplace(cleaned, out var platform))
        {
            return cleaned;
        }

        var credential = string.IsNullOrWhiteSpace(trackingId) ? string.Empty : trackingId.Trim();
        return platform switch
        {
            MarketplaceType.Kabum when KabumProductUrlParser.TryParse(cleaned, out var productId) =>
                KabumCommissionUrlBuilder.BuildProductAffiliateUrl(productId, credential),
            MarketplaceType.Amazon when AmazonProductUrlParser.TryParse(cleaned, out var asin) =>
                AmazonCommissionUrlBuilder.BuildProductAffiliateUrl(asin, credential),
            MarketplaceType.MagazineLuiza when MagazineLuizaProductUrlParser.TryParse(cleaned, out var magaluId) =>
                MagazineLuizaCommissionUrlBuilder.BuildProductAffiliateUrl(magaluId, credential),
            _ => cleaned
        };
    }

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

    private static bool TryGetHost(string? url, out string host)
    {
        host = string.Empty;
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        host = uri.Host.Trim().TrimStart('.').ToLower(CultureInfo.InvariantCulture);
        if (host.StartsWith("www.", StringComparison.Ordinal))
        {
            host = host[4..];
        }

        return host.Length > 0;
    }
}

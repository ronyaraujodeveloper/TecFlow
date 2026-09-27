using TecFlow.Business.Integrations;
using TecFlow.Business.Integrations.Amazon;
using TecFlow.Business.Integrations.CasasBahia;
using TecFlow.Business.Integrations.Kabum;
using TecFlow.Business.Integrations.MagazineLuiza;
using TecFlow.Business.Integrations.MercadoLivre;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>
/// Unshortener universal de agregadores (ofertou.ai, promoby.me, bit.ly, etc.):
/// até 10 saltos HTTP/JS/meta-refresh, mapeamento domínio → plataforma e injeção de comissão.
/// </summary>
public sealed class UniversalLinkResolverEngine
{
    public const int MaxHops = 10;

    private static readonly string[] AggregatorHosts =
    [
        "ofertou.ai",
        "promoby.me",
        "bit.ly",
        "tinyurl.com",
        "t.co",
        "t.me",
        "telegram.me",
        "cutt.ly",
        "is.gd",
        "ow.ly",
        "rb.gy",
        "shorturl.at"
    ];

    private static readonly (string Host, MarketplaceType Platform)[] DomainPlatformMap =
    [
        ("amazon.com.br", MarketplaceType.Amazon),
        ("amzn.to", MarketplaceType.Amazon),
        ("amzn.br", MarketplaceType.Amazon),
        ("a.co", MarketplaceType.Amazon),
        ("magazineluiza.com.br", MarketplaceType.MagazineLuiza),
        ("magazinevoce.com.br", MarketplaceType.MagazineLuiza),
        ("magalu.me", MarketplaceType.MagazineLuiza),
        ("magalu.com.br", MarketplaceType.MagazineLuiza),
        ("kabum.com.br", MarketplaceType.Kabum),
        ("shopee.com.br", MarketplaceType.Shopee),
        ("s.shopee.com.br", MarketplaceType.Shopee),
        ("br.shp.ee", MarketplaceType.Shopee),
        ("shp.ee", MarketplaceType.Shopee),
        ("mercadolivre.com.br", MarketplaceType.MercadoLivre),
        ("mercadolibre.com", MarketplaceType.MercadoLivre),
        ("meli.la", MarketplaceType.MercadoLivre),
        ("casasbahia.com.br", MarketplaceType.CasasBahia),
        ("tiktok.com", MarketplaceType.TikTokShop)
    ];

    private static readonly HashSet<string> CommissionAndTrackingKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "tag",
        "partner_id",
        "promoter_id",
        "pid",
        "c",
        "subid",
        "sub_id",
        "aff_id",
        "utm_source",
        "utm_medium",
        "utm_campaign",
        "utm_content",
        "utm_term",
        "utm_id",
        "affiliate_id",
        "an_id",
        "mmp_pid",
        "parceiro",
        "afiliado",
        "ref",
        "ref_",
        "click_id",
        "gclid",
        "fbclid"
    };

    private readonly IUrlExpansionService? _urlExpansionService;

    public UniversalLinkResolverEngine(IUrlExpansionService? urlExpansionService = null)
    {
        _urlExpansionService = urlExpansionService;
    }

    public async Task<string> ResolveFinalDestinationUrlAsync(string inputUrl) =>
        await ResolveFinalDestinationUrlAsync(inputUrl, CancellationToken.None);

    public async Task<string> ResolveFinalDestinationUrlAsync(
        string inputUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(inputUrl))
        {
            return inputUrl;
        }

        var workingUrl = AffiliateTrackingIdValidator.EnsureAbsoluteHttpUrl(inputUrl.Trim());
        if (_urlExpansionService is null)
        {
            return UrlUnshortenerService.IsSupportedMarketplaceUrl(workingUrl)
                ? StripCommissionAndTracking(workingUrl)
                : workingUrl;
        }

        return await UrlUnshortenerService.ResolveToFinalSupportedMarketplaceAsync(
            workingUrl,
            _urlExpansionService,
            cancellationToken);
    }

    public static bool ShouldExpand(string? url) =>
        !string.IsNullOrWhiteSpace(url);

    public static bool IsAggregatorUrl(string? url)
    {
        if (!TryNormalizeHost(url, out var host))
        {
            return false;
        }

        return AggregatorHosts.Any(item => host == item || host.EndsWith("." + item, StringComparison.Ordinal));
    }

    public static bool TryMapDomainToPlatform(string? url, out MarketplaceType platform)
    {
        platform = default;
        if (!TryNormalizeHost(url, out var host))
        {
            return false;
        }

        var bestLength = -1;
        MarketplaceType? best = null;
        foreach (var entry in DomainPlatformMap)
        {
            if (host != entry.Host && !host.EndsWith("." + entry.Host, StringComparison.Ordinal))
            {
                continue;
            }

            if (entry.Host.Length <= bestLength)
            {
                continue;
            }

            bestLength = entry.Host.Length;
            best = entry.Platform;
        }

        if (best is null)
        {
            return false;
        }

        platform = best.Value;
        return true;
    }

    public static string StripCommissionAndTracking(string? url)
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
            if (CommissionAndTrackingKeys.Contains(key))
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
        var cleaned = StripCommissionAndTracking(storeUrl);
        if (!TryMapDomainToPlatform(cleaned, out var platform))
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
            MarketplaceType.CasasBahia when CasasBahiaProductUrlParser.TryParse(cleaned, out var casasId) =>
                CasasBahiaCommissionUrlBuilder.BuildProductAffiliateUrl(casasId, credential, credential),
            MarketplaceType.MercadoLivre when MercadoLivreProductUrlParser.TryParse(cleaned, out var itemId) =>
                MercadoLivreCommissionUrlBuilder.BuildCatalogAffiliateUrl(itemId, credential, "tecflow"),
            MarketplaceType.TikTokShop when TikTokShopProductUrlParser.TryParse(cleaned, out var tiktokId) =>
                TikTokShopCommissionUrlBuilder.BuildProductAffiliateUrl(tiktokId, credential),
            MarketplaceType.Shopee when ShopeeProductUrlParser.TryParse(cleaned, out var shopeeIds) =>
                ShopeeCommissionUrlBuilder.BuildUniversalDeeplink(shopeeIds, credential),
            _ => cleaned
        };
    }

    public static string? TryExtractRedirectFromHtml(string? html, string currentUrl) =>
        UrlUnshortenerService.TryExtractRedirectFromHtml(html, currentUrl);

    private static bool TryNormalizeHost(string? url, out string host)
    {
        host = string.Empty;
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(AffiliateTrackingIdValidator.EnsureAbsoluteHttpUrl(url.Trim()), UriKind.Absolute, out var uri))
        {
            return false;
        }

        host = uri.Host.Trim().TrimStart('.').ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
        {
            host = host[4..];
        }

        return host.Length > 0;
    }
}

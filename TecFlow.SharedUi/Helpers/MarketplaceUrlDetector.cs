using TecFlow.Core.Enums;

namespace TecFlow.SharedUi.Helpers;

/// <summary>Detecção client-side de marketplace a partir da URL colada pelo usuário.</summary>
public enum SupportedMarketplaceKey
{
    Shopee,
    TikTokShop,
    Amazon,
    MercadoLivre,
    Magalu,
    Kabum,
    CasasBahia
}

public sealed record MarketplaceDefinition(
    SupportedMarketplaceKey Key,
    string DisplayName,
    string ShortLabel,
    string CssClass,
    string[] HostPatterns,
    MarketplaceType? IntegrationType,
    bool IsBackendReady);

public static class MarketplaceUrlDetector
{
    public static IReadOnlyList<MarketplaceDefinition> All { get; } =
    [
        new(
            SupportedMarketplaceKey.Shopee,
            "Shopee",
            "SP",
            "marketplace-chip--shopee",
            ["shopee.com", "shopee.com.br", "shope.ee", "shp.ee", "br.shp.ee", "s.shopee.com.br"],
            MarketplaceType.Shopee,
            true),
        new(
            SupportedMarketplaceKey.TikTokShop,
            "TikTok Shop",
            "TT",
            "marketplace-chip--tiktok",
            ["tiktok.com", "tiktokshop.com", "shop.tiktok.com", "vm.tiktok.com", "vt.tiktok.com", "l.tiktok.com"],
            MarketplaceType.TikTokShop,
            true),
        new(
            SupportedMarketplaceKey.Amazon,
            "Amazon",
            "AM",
            "marketplace-chip--amazon",
            ["amazon.com", "amazon.com.br", "amzn.to", "a.co"],
            MarketplaceType.Amazon,
            true),
        new(
            SupportedMarketplaceKey.MercadoLivre,
            "Mercado Livre",
            "ML",
            "marketplace-chip--mercadolivre",
            ["mercadolivre.com.br", "produto.mercadolivre.com.br", "mercadolivre.com", "mercadolibre.com", "ml.com.br"],
            MarketplaceType.MercadoLivre,
            true),
        new(
            SupportedMarketplaceKey.Magalu,
            "Magazine Luiza",
            "MG",
            "marketplace-chip--magalu",
            ["magazineluiza.com.br", "magalu.com.br", "magazinevoce.com.br", "magalu.me", "mglz.ne"],
            MarketplaceType.MagazineLuiza,
            true),
        new(
            SupportedMarketplaceKey.Kabum,
            "Kabum!",
            "KB",
            "marketplace-chip--kabum",
            ["kabum.com.br", "kb.um", "kabum.me"],
            MarketplaceType.Kabum,
            true),
        new(
            SupportedMarketplaceKey.CasasBahia,
            "Casas Bahia",
            "CB",
            "marketplace-chip--casasbahia",
            ["casasbahia.com.br", "cb.com.br", "casasbahia.app.link"],
            MarketplaceType.CasasBahia,
            true)
    ];

    public static MarketplaceDefinition? Detect(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var normalized = NormalizeUrl(url);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var host = uri.Host.ToLowerInvariant();

        foreach (var definition in All)
        {
            if (HostMatches(host, definition.HostPatterns))
            {
                return definition;
            }
        }

        return null;
    }

    public static MarketplaceDefinition? GetByKey(SupportedMarketplaceKey key) =>
        All.FirstOrDefault(item => item.Key == key);

    private static string NormalizeUrl(string url)
    {
        var trimmed = url.Trim();
        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + trimmed;
        }

        return trimmed;
    }

    private static bool HostMatches(string host, IEnumerable<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (host.Equals(pattern, StringComparison.OrdinalIgnoreCase)
                || host.EndsWith("." + pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

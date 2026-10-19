using Ofertio.Core.Entities;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Service.Groups;

public static class OfferPlatformFilterRules
{
    public const string AllPlatformsLabel = "Todas as Plataformas";

    public const string OthersFilter = "Outros";

    public static readonly MarketplaceType[] ListedPlatforms =
    [
        MarketplaceType.MercadoLivre,
        MarketplaceType.Shopee,
        MarketplaceType.Amazon,
        MarketplaceType.MagazineLuiza,
        MarketplaceType.AliExpress,
        MarketplaceType.CasasBahia
    ];

    public static readonly IReadOnlyList<(string Value, string Label)> Options =
    [
        (nameof(MarketplaceType.MercadoLivre), "Mercado Livre"),
        (nameof(MarketplaceType.Shopee), "Shopee"),
        (nameof(MarketplaceType.Amazon), "Amazon"),
        (nameof(MarketplaceType.MagazineLuiza), "Magazine Luiza"),
        (nameof(MarketplaceType.AliExpress), "AliExpress"),
        (nameof(MarketplaceType.CasasBahia), "Casas Bahia / Ponto"),
        (OthersFilter, "Outros")
    ];

    public static string? Normalize(string? platform)
    {
        if (string.IsNullOrWhiteSpace(platform))
        {
            return null;
        }

        var value = platform.Trim();
        if (string.Equals(value, OthersFilter, StringComparison.OrdinalIgnoreCase))
        {
            return OthersFilter;
        }

        if (string.Equals(value, "Magalu", StringComparison.OrdinalIgnoreCase))
        {
            return nameof(MarketplaceType.MagazineLuiza);
        }

        if (string.Equals(value, "Ponto", StringComparison.OrdinalIgnoreCase))
        {
            return nameof(MarketplaceType.CasasBahia);
        }

        return !int.TryParse(value, out _)
            && Enum.TryParse<MarketplaceType>(value, ignoreCase: true, out var parsed)
            && ListedPlatforms.Contains(parsed)
                ? parsed.ToString()
                : null;
    }

    public static IQueryable<GroupCapturedMessage> Apply(IQueryable<GroupCapturedMessage> query, string? platform)
    {
        var normalized = Normalize(platform);
        if (normalized is null)
        {
            return query;
        }

        if (normalized == OthersFilter)
        {
            var listed = ListedPlatforms;
            return query.Where(item => item.PlatformType == null || !listed.Contains(item.PlatformType.Value));
        }

        var selected = Enum.Parse<MarketplaceType>(normalized);
        return query.Where(item => item.PlatformType == selected);
    }
}

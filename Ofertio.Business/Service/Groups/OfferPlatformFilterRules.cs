using Ofertio.Business.Dto;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Service.Groups;

public static class OfferPlatformFilterRules
{
    public const string AllPlatformsLabel = "Todas as Plataformas";

    public static readonly MarketplaceType[] DefaultPlatforms =
    [
        MarketplaceType.MercadoLivre,
        MarketplaceType.Shopee
    ];

    private static readonly MarketplaceType[] DisplayOrder =
    [
        MarketplaceType.MercadoLivre,
        MarketplaceType.Shopee,
        MarketplaceType.Amazon,
        MarketplaceType.MagazineLuiza,
        MarketplaceType.AliExpress,
        MarketplaceType.CasasBahia,
        MarketplaceType.Kabum,
        MarketplaceType.TikTokShop
    ];

    /// <summary>
    /// Plataformas conectadas (contas ativas) têm prioridade; sem conta conectada usa as plataformas detectadas
    /// nas ofertas capturadas e, sem nenhuma, as plataformas padrão.
    /// </summary>
    public static List<PlatformFilterOptionDto> BuildOptions(
        IEnumerable<MarketplaceType>? connected,
        IEnumerable<MarketplaceType>? detected = null)
    {
        var source = Defined(connected);
        if (source.Count == 0)
        {
            source = Defined(detected);
        }

        if (source.Count == 0)
        {
            source = DefaultPlatforms.ToHashSet();
        }

        return source
            .OrderBy(platform => Array.IndexOf(DisplayOrder, platform) is var index and >= 0 ? index : int.MaxValue)
            .ThenBy(platform => platform.ToString(), StringComparer.Ordinal)
            .Select(ToOption)
            .ToList();
    }

    public static PlatformFilterOptionDto ToOption(MarketplaceType platform) => new()
    {
        Value = platform.ToString(),
        Label = Label(platform)
    };

    public static string Label(MarketplaceType platform) =>
        platform == MarketplaceType.CasasBahia ? "Casas Bahia / Ponto" : platform.GetDisplayName();

    public static string? Normalize(string? platform)
    {
        if (string.IsNullOrWhiteSpace(platform) || int.TryParse(platform.Trim(), out _))
        {
            return null;
        }

        if (string.Equals(platform.Trim(), "Ponto", StringComparison.OrdinalIgnoreCase))
        {
            return nameof(MarketplaceType.CasasBahia);
        }

        if (!MarketplaceTypeExtensions.TryCanonicalKey(platform, out var key))
        {
            return null;
        }

        foreach (var candidate in Enum.GetValues<MarketplaceType>())
        {
            if (candidate.CanonicalKey() == key)
            {
                return candidate.ToString();
            }
        }

        return null;
    }

    public static IQueryable<GroupCapturedMessage> Apply(IQueryable<GroupCapturedMessage> query, string? platform)
    {
        var normalized = Normalize(platform);
        if (normalized is null)
        {
            return query;
        }

        var selected = Enum.Parse<MarketplaceType>(normalized);
        return query.Where(item => item.PlatformType == selected);
    }

    private static HashSet<MarketplaceType> Defined(IEnumerable<MarketplaceType>? platforms) =>
        platforms is null
            ? []
            : platforms.Where(Enum.IsDefined).ToHashSet();
}

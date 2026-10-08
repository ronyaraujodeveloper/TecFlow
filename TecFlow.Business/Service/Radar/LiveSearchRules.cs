using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.Radar;

public static class LiveSearchRules
{
    public static bool Matches(
        string? title,
        decimal? price,
        string? coupon,
        string? platformName,
        MarketplaceType? platformType,
        string? keyword,
        decimal? minPrice,
        decimal? maxPrice,
        bool? hasCoupon,
        string? store)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var needle = keyword.Trim();
            var haystack = string.Join(' ', title, coupon, platformName);
            if (haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }
        }

        if (minPrice is > 0 && (price is null || price < minPrice))
        {
            return false;
        }

        if (maxPrice is > 0 && (price is null || price > maxPrice))
        {
            return false;
        }

        if (hasCoupon == true && string.IsNullOrWhiteSpace(coupon))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(store)
            && !StoreMatches(store, platformName, platformType))
        {
            return false;
        }

        return true;
    }

    public static bool StoreMatches(string store, string? platformName, MarketplaceType? platformType)
    {
        var needle = store.Trim();
        if (platformType is { } type && type.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(platformName)
            && platformName.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsCheaper(decimal? captured, decimal? live) =>
        live is > 0 && (captured is not > 0 || live.Value + 0.009m < captured.Value);

    public static string MapListingStatus(string? raw, bool available)
    {
        if (!available)
        {
            return GroupOfferStatuses.Esgotado;
        }

        var status = (raw ?? string.Empty).Trim().ToLowerInvariant();
        if (status is "closed" || status is "paused" || status is "inactive" || status is "deleted"
            || status is "ended" || status is "out_of_stock")
        {
            return GroupOfferStatuses.Esgotado;
        }

        return GroupOfferStatuses.Ativo;
    }
}

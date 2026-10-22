using Ofertio.Business.Dto;

namespace Ofertio.SharedUi.Helpers;

public static class StorePriceComparisonRequests
{
    public static StorePriceComparisonRequestDto FromCatalog(OfficialCatalogProductDto item) =>
        new()
        {
            ProductName = item.ProductName,
            ImageUrl = item.ImageUrl,
            SourceUrl = item.SourceUrl,
            PlatformType = item.PlatformType,
            PlatformName = item.Platform,
            Price = item.Price,
            Shipping = item.Shipping,
            CommissionPercent = item.CommissionPercent,
            EstimatedCommission = item.EstimatedCommission
        };

    public static StorePriceComparisonRequestDto FromCaptured(GroupCapturedOfferDto item) =>
        new()
        {
            ProductName = item.ProductName,
            ImageUrl = item.ProductImageUrl,
            SourceUrl = string.IsNullOrWhiteSpace(item.PrimaryProductUrl) ? item.OriginalUrl : item.PrimaryProductUrl,
            PlatformType = item.PlatformType,
            PlatformName = item.PlatformName,
            Price = item.ValidatedPrice ?? item.Price
        };

    public static StorePriceComparisonRequestDto FromRadar(OfferRadarItemDto item) =>
        new()
        {
            ProductName = item.ProductName,
            ImageUrl = item.ProductImageUrl,
            SourceUrl = string.IsNullOrWhiteSpace(item.AffiliateUrl) ? item.OriginalUrl : item.AffiliateUrl,
            PlatformType = item.PlatformType,
            PlatformName = item.PlatformName,
            Price = item.Price
        };
}

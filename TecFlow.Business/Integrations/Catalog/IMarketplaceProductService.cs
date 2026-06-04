using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Shopee.Payloads;
using TecFlow.Business.Integrations.TikTokShop.Payloads;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Integrations.Catalog;

public interface IMarketplaceProductService
{
    Task<ProductResponseDto> FetchProductsFromPlatformAsync(
        string shopId,
        MarketplaceType type,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    ProductResponseDto ConvertToInternalProductDto(
        MarketplaceType marketplace,
        ShopeeGetItemBaseInfoResponsePayload shopeePayload,
        IReadOnlyDictionary<long, ShopeeGetModelListResponsePayload>? modelsByItemId = null);

    ProductResponseDto ConvertToInternalProductDto(
        MarketplaceType marketplace,
        TikTokShopProductsSearchResponsePayload tikTokPayload);
}

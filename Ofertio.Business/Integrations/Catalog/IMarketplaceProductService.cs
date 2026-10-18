using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Shopee.Payloads;
using Ofertio.Business.Integrations.TikTokShop.Payloads;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Integrations.Catalog;

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

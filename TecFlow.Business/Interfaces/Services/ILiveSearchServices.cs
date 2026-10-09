using TecFlow.Business.Dto;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database.Filter;

namespace TecFlow.Business.Interfaces.Services;

public interface IMercadoLivreApiService
{
    Task<OfficialOfferSnapshotDto?> GetItemAsync(string itemId, CancellationToken cancellationToken = default);

    Task<OfficialCatalogChannelResult> SearchProductsAsync(
        int userId,
        string query,
        int limit = 20,
        string? trackingId = null,
        CancellationToken cancellationToken = default);
}

public interface IShopeeAffiliateOfferService
{
    Task<OfficialOfferSnapshotDto?> GetProductOfferAsync(
        int userId,
        string shopId,
        string itemId,
        CancellationToken cancellationToken = default);

    Task<OfficialCatalogChannelResult> SearchProductsAsync(
        int userId,
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default);
}

public interface IAmazonPaApiService
{
    Task<OfficialOfferSnapshotDto?> GetItemAsync(
        int userId,
        string asin,
        CancellationToken cancellationToken = default);

    Task<OfficialCatalogChannelResult> SearchProductsAsync(
        int userId,
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default);
}

public interface IOfficialCatalogSearchService
{
    Task<OfficialCatalogSearchResponseDto> SearchAsync(
        int userId,
        OfficialCatalogSearchFilter filter,
        CancellationToken cancellationToken = default);
}

public interface ILiveCheckSearchService
{
    Task<OfficialOfferSnapshotDto> CheckAndPersistAsync(
        GroupCapturedMessage entity,
        int userId,
        CancellationToken cancellationToken = default);

    Task<OfficialOfferSnapshotDto> CheckUrlAsync(
        int userId,
        string? url,
        MarketplaceType? platform,
        decimal? capturedPrice,
        CancellationToken cancellationToken = default);
}

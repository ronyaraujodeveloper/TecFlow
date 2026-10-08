using TecFlow.Business.Dto;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Interfaces.Services;

public interface IPriceHistoryTracker
{
    Task RecordAsync(
        string? url,
        MarketplaceType? platform,
        decimal? price,
        DateTime capturedAt,
        CancellationToken cancellationToken = default);

    Task EnrichOffersAsync(IList<GroupCapturedOfferDto> offers, CancellationToken cancellationToken = default);
}

public interface IDealCreditsService
{
    Task<DealCreditsResponseDto> ListShowcaseAsync(
        int userId,
        LiveSearchFilterDto? search = null,
        CancellationToken cancellationToken = default);

    Task<DealCreditsResponseDto> UnlockAsync(int userId, int dealId, CancellationToken cancellationToken = default);

    Task<DealCreditsResponseDto> TopUpAsync(int userId, int packSize, CancellationToken cancellationToken = default);

    Task<int> GrantDailyQuotasAsync(CancellationToken cancellationToken = default);
}

public interface IGlobalTrendingDealsEngine
{
    Task RunCycleAsync(CancellationToken cancellationToken = default);
}

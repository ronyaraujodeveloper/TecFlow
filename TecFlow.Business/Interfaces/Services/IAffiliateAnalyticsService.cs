using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Services;

public interface IAffiliateAnalyticsService
{
    Task<IReadOnlyList<MarketplaceCommissionLineDto>> FetchMarketplaceCommissionsAsync(
        int ownerId,
        string affiliateId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default);

    Task<(AffiliatePerformanceDto Performance, IReadOnlyList<CommissionDiscrepancyReportDto> Discrepancies)> ReconcileCommissionsAsync(
        int ownerId,
        string affiliateId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default);
}

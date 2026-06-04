using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class AffiliatePerformanceDto
{
    public string AffiliateId { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public long TotalClicks { get; set; }
    public int TotalConversions { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal EstimatedCommission { get; set; }
    public decimal PaidCommission { get; set; }
    public decimal RetainedAmount { get; set; }
    public int DiscrepancyCount { get; set; }
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public MarketplaceType? PrimaryMarketplace { get; set; }
}

using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class CommissionDiscrepancyReportDto
{
    public int LineId { get; set; }
    public string ExternalOrderId { get; set; } = string.Empty;
    public string TrackingCode { get; set; } = string.Empty;
    public MarketplaceType Marketplace { get; set; }
    public decimal ExpectedCommission { get; set; }
    public decimal PaidCommission { get; set; }
    public decimal Difference { get; set; }
    public CommissionStatus Status { get; set; }
    public bool IsDivergent { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? AffiliateLinkId { get; set; }
    public string? ProductSku { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}

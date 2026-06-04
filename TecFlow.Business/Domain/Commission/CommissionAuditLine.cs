using TecFlow.Core.Enums;

namespace TecFlow.Business.Domain.Commission;

/// <summary>Linha de comissão para auditoria (marketplace vs. registo local).</summary>
public class CommissionAuditLine
{
    public string ExternalOrderId { get; set; } = string.Empty;
    public MarketplaceType Marketplace { get; set; }
    public decimal MarketplaceReportedAmount { get; set; }
    public decimal? LocalRecordedAmount { get; set; }
    public CommissionStatus Status { get; set; }
    public string? ProductSku { get; set; }
    public int? AffiliateLinkId { get; set; }
    public bool HasDiscrepancy { get; set; }
    public string? DiscrepancyReason { get; set; }
}

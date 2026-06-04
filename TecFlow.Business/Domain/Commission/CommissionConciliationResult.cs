namespace TecFlow.Business.Domain.Commission;

public class CommissionConciliationResult
{
    public bool Success { get; set; }
    public string Summary { get; set; } = string.Empty;
    public int TotalLines { get; set; }
    public int MatchedLines { get; set; }
    public int DiscrepancyCount { get; set; }
    public decimal TotalMarketplaceAmount { get; set; }
    public decimal TotalLocalAmount { get; set; }
    public IReadOnlyList<CommissionAuditLine> Lines { get; set; } = Array.Empty<CommissionAuditLine>();
}

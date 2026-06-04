namespace TecFlow.Business.Dto;

public class AffiliateReconciliationResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public AffiliatePerformanceDto? Performance { get; set; }
    public List<CommissionDiscrepancyReportDto>? Discrepancies { get; set; }
    public PagingInfoDto? Paging { get; set; }
}

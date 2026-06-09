namespace TecFlow.Database.Filter;

public class AffiliateReconciliationFilter : IPagedFilter
{
    public string? AffiliateId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? LojaId { get; set; }
}

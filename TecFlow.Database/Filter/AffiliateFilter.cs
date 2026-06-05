using TecFlow.Database.Pagin;

namespace TecFlow.Database.Filter;

public class AffiliateFilter : IPagedFilter
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = PagedListHelper.DefaultPageSize;

    public int? Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? AffiliateCode { get; set; }
    public decimal? Commission { get; set; }
    public int? CampaignId { get; set; }
    public int? ContentId { get; set; }
    public int? OwnerId { get; set; }
}

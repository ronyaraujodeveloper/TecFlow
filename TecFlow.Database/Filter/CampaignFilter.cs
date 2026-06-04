using TecFlow.Database.Pagin;

namespace TecFlow.Database.Filter;

public class CampaignFilter : IPagedFilter
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = PagedListHelper.DefaultPageSize;

    public int? Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Budget { get; set; }
    public int? OwnerId { get; set; }
}

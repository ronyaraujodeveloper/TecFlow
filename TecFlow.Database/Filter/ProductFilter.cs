using TecFlow.Database.Pagin;

namespace TecFlow.Database.Filter;

public class ProductFilter : IPagedFilter
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = PagedListHelper.DefaultPageSize;

    public int? Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Name { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal? Price { get; set; }
    public decimal? SalesVolume { get; set; }
    public double? Rating { get; set; }
    public int? Stock { get; set; }
    public int? OwnerId { get; set; }
    public Guid? TenantId { get; set; }
    public string? ShopId { get; set; }
}

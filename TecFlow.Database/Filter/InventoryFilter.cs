using TecFlow.Database.Pagin;

namespace TecFlow.Database.Filter;

public class InventoryFilter : IPagedFilter
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = PagedListHelper.DefaultPageSize;

    public int? Id { get; set; }
    public int? ProductId { get; set; }
    public Guid? TenantId { get; set; }
    public int? MinimumStock { get; set; }
    public bool? BelowMinimumOnly { get; set; }
}

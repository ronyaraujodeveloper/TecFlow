using TecFlow.Core.Enums;
using TecFlow.Database.Pagin;

namespace TecFlow.Database.Filter;

public class SalesOrderFilter : IPagedFilter
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = PagedListHelper.DefaultPageSize;

    public Guid? Id { get; set; }
    public Guid? TenantId { get; set; }
    public string? OrderNumber { get; set; }
    public string? ShopId { get; set; }
    public int? CustomerId { get; set; }
    public OrderStatus? Status { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
}

using Ofertio.Database.Pagin;

namespace Ofertio.Database.Filter;

public class CustomerFilter : IPagedFilter
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = PagedListHelper.DefaultPageSize;

    public int? Id { get; set; }
    public Guid? TenantId { get; set; }
    public string? Name { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int? CityId { get; set; }
    public int? StateId { get; set; }
}

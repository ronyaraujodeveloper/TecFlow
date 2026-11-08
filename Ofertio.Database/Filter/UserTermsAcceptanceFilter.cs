using Ofertio.Database.Pagin;

namespace Ofertio.Database.Filter;

public class UserTermsAcceptanceFilter : IPagedFilter
{
    public int? UserId { get; set; }
    public string? DocumentKind { get; set; }
    public string? Purpose { get; set; }
    public string? Version { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = PagedListHelper.DefaultPageSize;
}

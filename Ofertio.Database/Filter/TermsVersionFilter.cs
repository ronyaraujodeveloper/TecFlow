using Ofertio.Database.Pagin;

namespace Ofertio.Database.Filter;

public class TermsVersionFilter : IPagedFilter
{
    public string? DocumentKind { get; set; }
    public string? Version { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = PagedListHelper.DefaultPageSize;
}

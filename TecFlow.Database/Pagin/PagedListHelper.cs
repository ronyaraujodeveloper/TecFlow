using TecFlow.Database.Filter;

namespace TecFlow.Database.Pagin;

public static class PagedListHelper
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 30;

    public static int NormalizePage(int page) => page < 1 ? 1 : page;

    public static int NormalizePageSize(int pageSize)
    {
        if (pageSize <= 0)
        {
            return DefaultPageSize;
        }

        return pageSize > MaxPageSize ? MaxPageSize : pageSize;
    }

    public static (List<T> Items, PagedListMeta Meta) Slice<T>(IEnumerable<T> source, IPagedFilter filter)
    {
        var page = NormalizePage(filter.Page);
        var pageSize = NormalizePageSize(filter.PageSize);
        var list = source as IList<T> ?? source.ToList();
        var totalCount = list.Count;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return (items, new PagedListMeta
        {
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = page < totalPages
        });
    }
}

public class PagedListMeta
{
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
}

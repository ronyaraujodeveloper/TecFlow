namespace TecFlow.Business.Dto;

/// <summary>Metadados de paginação no envelope ResponseDto (scroll infinito / toque).</summary>
public class PagingInfoDto
{
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }

    public static PagingInfoDto FromMeta(TecFlow.Database.Pagin.PagedListMeta meta) => new()
    {
        TotalCount = meta.TotalCount,
        CurrentPage = meta.CurrentPage,
        PageSize = meta.PageSize,
        TotalPages = meta.TotalPages,
        HasNextPage = meta.HasNextPage
    };
}

namespace TecFlow.Database.Filter;

/// <summary>Parâmetros de paginação para listagens consumidas por WebUi e clientes móveis.</summary>
public interface IPagedFilter
{
    int Page { get; set; }
    int PageSize { get; set; }
}

using TecFlow.Core.Enums;
using TecFlow.Database.Pagin;

namespace TecFlow.Database.Filter;

/// <summary>Filtro escalar para futuras listagens de links de afiliado gerados.</summary>
public class AffiliateLinkFilter : IPagedFilter
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = PagedListHelper.DefaultPageSize;

    /// <summary>Reservado. O histórico GET ignora este campo para não ocultar outras plataformas.</summary>
    public int? LojaId { get; set; }

    public MarketplaceType? PlatformType { get; set; }
}

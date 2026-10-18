using Ofertio.Core.Enums;
using Ofertio.Database.Pagin;

namespace Ofertio.Database.Filter;

public class IntegracaoLojaFilter : IPagedFilter
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = PagedListHelper.DefaultPageSize;

    public int? Id { get; set; }
    public int? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public MarketplaceType? PlatformType { get; set; }
    public MarketplaceIntegrationStatus? Status { get; set; }
    public string? FriendlyName { get; set; }
    public string? ShopId { get; set; }
}

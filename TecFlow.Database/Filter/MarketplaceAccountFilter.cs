using TecFlow.Core.Enums;

namespace TecFlow.Database.Filter;

public class MarketplaceAccountFilter
{
    public int? Id { get; set; }
    public Guid? TenantId { get; set; }
    public MarketplaceType? MarketplaceType { get; set; }
    public string? ShopId { get; set; }
    public string? ShopName { get; set; }
    public string? Cnpj { get; set; }
}

using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class MarketplaceAccountDto
{
    public Guid TenantId { get; set; }
    public MarketplaceType MarketplaceType { get; set; }
    public string ShopId { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string? Cnpj { get; set; }
}

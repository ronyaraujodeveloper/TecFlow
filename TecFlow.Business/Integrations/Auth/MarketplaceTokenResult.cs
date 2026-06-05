using TecFlow.Core.Enums;

namespace TecFlow.Business.Integrations.Auth;

public class MarketplaceTokenResult
{
    public bool Success { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
    public MarketplaceType MarketplaceType { get; set; }
    public DateTime ExpiresAt { get; set; }
}

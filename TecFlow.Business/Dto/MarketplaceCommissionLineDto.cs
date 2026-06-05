using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

/// <summary>Linha importada do relatório de comissões do marketplace.</summary>
public class MarketplaceCommissionLineDto
{
    public string ExternalOrderId { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
    public MarketplaceType Marketplace { get; set; }
    public string TrackingCode { get; set; } = string.Empty;
    public decimal PaidCommission { get; set; }
    public decimal OrderAmount { get; set; }
    public int Clicks { get; set; }
    public int Conversions { get; set; }
    public string? ProductSku { get; set; }
    public DateTime ReportedAtUtc { get; set; }
    public bool IsRetained { get; set; }
}

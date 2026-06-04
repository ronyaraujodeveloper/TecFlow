using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;

namespace TecFlow.Core.Entities;

[Table("MarketplaceOrderLines")]
public class MarketplaceOrderLine : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }

    public int MarketplaceOrderId { get; set; }
    public MarketplaceOrder? MarketplaceOrder { get; set; }

    [MaxLength(128)]
    public string? ExternalSkuId { get; set; }

    [MaxLength(128)]
    public string SkuCode { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? ExternalProductId { get; set; }

    public int Quantity { get; set; }
}

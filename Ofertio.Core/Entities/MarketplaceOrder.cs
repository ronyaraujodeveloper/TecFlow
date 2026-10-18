using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ofertio.Core.Abstractions;
using Ofertio.Core.Enums;

namespace Ofertio.Core.Entities;

[Table("MarketplaceOrders")]
public class MarketplaceOrder : BaseEntity, ITenantScopedEntity, IShopScopedEntity
{
    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(128)]
    public required string ExternalOrderId { get; set; }

    [Required]
    [MaxLength(128)]
    public required string ShopId { get; set; }

    public MarketplaceType MarketplaceType { get; set; }

    [MaxLength(64)]
    public string Status { get; set; } = string.Empty;

    public bool StockDeducted { get; set; }

    public DateTime ProcessedAt { get; set; }

    public ICollection<MarketplaceOrderLine> Lines { get; set; } = new List<MarketplaceOrderLine>();
}

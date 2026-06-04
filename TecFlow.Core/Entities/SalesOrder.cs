using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;
using TecFlow.Core.Enums;

namespace TecFlow.Core.Entities;

/// <summary>Pedido de venda direta (ERP local). Distinto de <see cref="MarketplaceOrder"/>.</summary>
[Table("SalesOrders")]
public class SalesOrder : ITenantScopedEntity, IShopScopedEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(32)]
    public string OrderNumber { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    [MaxLength(128)]
    public string ShopId { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FreightAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pendente;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();

    public void Touch() => UpdatedAt = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;
using TecFlow.Core.Enums;

namespace TecFlow.Core.Entities;

/// <summary>Histórico (kardex) de movimentações de estoque.</summary>
[Table("InventoryMovements")]
public class InventoryMovement : ITenantScopedEntity
{
    [Key]
    public int Id { get; set; }

    public Guid TenantId { get; set; }

    public int InventoryId { get; set; }

    public Inventory? Inventory { get; set; }

    public int Quantity { get; set; }

    public InventoryMovementType MovementType { get; set; }

    [MaxLength(512)]
    public string Description { get; set; } = string.Empty;

    public Guid? SalesOrderId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;

namespace TecFlow.Core.Entities;

/// <summary>Estoque físico por produto e tenant.</summary>
[Table("Inventories")]
public class Inventory : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int PhysicalQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public int MinimumStock { get; set; }

    [NotMapped]
    public int AvailableQuantity => Math.Max(0, PhysicalQuantity - ReservedQuantity);

    public ICollection<InventoryMovement> Movements { get; set; } = new List<InventoryMovement>();
}

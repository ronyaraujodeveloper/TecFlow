using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;

namespace TecFlow.Core.Entities;

/// <summary>Cliente do pedido de venda direta.</summary>
[Table("Customers")]
public class Customer : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(18)]
    public string? DocumentNumber { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(256)]
    public string Street { get; set; } = string.Empty;

    [MaxLength(32)]
    public string StreetNumber { get; set; } = string.Empty;

    [MaxLength(9)]
    public string ZipCode { get; set; } = string.Empty;

    [MaxLength(128)]
    public string City { get; set; } = string.Empty;

    [MaxLength(2)]
    public string State { get; set; } = string.Empty;

    public ICollection<SalesOrder> Orders { get; set; } = new List<SalesOrder>();
}

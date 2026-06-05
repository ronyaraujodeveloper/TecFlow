using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

/// <summary>Conta corporativa / assinante SaaS do ecossistema TecFlow.</summary>
[Table("Tenants")]
public class Tenant
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? DocumentNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<MarketplaceAccount> MarketplaceAccounts { get; set; } =
        new List<MarketplaceAccount>();
}

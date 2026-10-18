using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ofertio.Core.Entities;

/// <summary>Conta corporativa / assinante SaaS do ecossistema Ofertio.</summary>
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

    /// <summary>Ativo, Trial, Inadimplente, Pausado, Suspenso ou Cancelado.</summary>
    [MaxLength(24)]
    public string Status { get; set; } = "Ativo";

    [MaxLength(512)]
    public string? StatusReason { get; set; }

    public DateTime? StatusChangedAt { get; set; }

    /// <summary>Código do <see cref="SaasPlan"/>; quando nulo usa o plano do usuário principal.</summary>
    [MaxLength(32)]
    public string? PlanCode { get; set; }

    /// <summary>Mensal ou Anual.</summary>
    [MaxLength(16)]
    public string BillingCycle { get; set; } = "Mensal";

    public DateTime? TrialEndsAt { get; set; }

    public DateTime? RenewalAt { get; set; }

    public DateTime? CanceledAt { get; set; }

    [MaxLength(64)]
    public string? GatewayCustomerId { get; set; }

    [MaxLength(64)]
    public string? GatewaySubscriptionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<MarketplaceAccount> MarketplaceAccounts { get; set; } =
        new List<MarketplaceAccount>();
}

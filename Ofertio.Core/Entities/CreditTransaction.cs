using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ofertio.Core.Abstractions;

namespace Ofertio.Core.Entities;

[Table("CreditTransactions")]
public class CreditTransaction : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }

    public int Amount { get; set; }

    [Required]
    [MaxLength(32)]
    public string Kind { get; set; } = string.Empty;

    public int? DealId { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

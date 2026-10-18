using System.ComponentModel.DataAnnotations.Schema;
using Ofertio.Core.Abstractions;

namespace Ofertio.Core.Entities;

[Table("TenantDealUnlocks")]
public class TenantDealUnlock : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }

    public int DealId { get; set; }

    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
}

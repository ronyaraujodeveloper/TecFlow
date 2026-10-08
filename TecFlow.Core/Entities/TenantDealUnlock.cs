using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;

namespace TecFlow.Core.Entities;

[Table("TenantDealUnlocks")]
public class TenantDealUnlock : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }

    public int DealId { get; set; }

    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
}

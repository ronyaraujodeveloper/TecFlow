using System.ComponentModel.DataAnnotations.Schema;
using Ofertio.Core.Abstractions;

namespace Ofertio.Core.Entities;

[Table("TenantCredits")]
public class TenantCredit : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }

    public int DailyBalance { get; set; }

    public int PurchasedBalance { get; set; }

    public DateTime? DailyGrantedOnUtc { get; set; }
}

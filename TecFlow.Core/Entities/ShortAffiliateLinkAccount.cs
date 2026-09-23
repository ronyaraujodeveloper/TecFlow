using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;

namespace TecFlow.Core.Entities;

/// <summary>
/// Associação de uma conta/loja a um grupo de conversão de link.
/// Desmarcar a conta define <see cref="IsActive"/> = false; o registro não é excluído.
/// </summary>
[Table("ShortAffiliateLinkAccounts")]
public class ShortAffiliateLinkAccount : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }

    public Guid LinkGroupId { get; set; }

    public int ShortAffiliateLinkId { get; set; }

    public ShortAffiliateLink? AffiliateLink { get; set; }

    public int IntegracaoLojaId { get; set; }

    public int? MarketplaceAccountId { get; set; }

    public bool IsActive { get; set; } = true;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;
using TecFlow.Core.Enums;

namespace TecFlow.Core.Entities;

/// <summary>Vínculo de múltiplas lojas (ShopId) por tenant em cada marketplace.</summary>
[Table("MarketplaceAccounts")]
public class MarketplaceAccount : BaseEntity, ITenantScopedEntity, IShopScopedEntity
{
    public Guid TenantId { get; set; }

    public Tenant? Tenant { get; set; }

    /// <summary>Identificador do usuário TecFlow (string para alinhar claims/JWT).</summary>
    [Required]
    [MaxLength(128)]
    public string UserId { get; set; } = string.Empty;

    public MarketplaceType MarketplaceType { get; set; }

    [Required]
    [MaxLength(256)]
    public string FriendlyName { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ShopId { get; set; } = string.Empty;

    [MaxLength(256)]
    public string ShopName { get; set; } = string.Empty;

    /// <summary>ID de afiliado / Tracking ID da Shopee (ex.: 18325850271).</summary>
    [MaxLength(64)]
    public string? AffiliateTrackingId { get; set; }

    [Required]
    public string AccessToken { get; set; } = string.Empty;

    public string? RefreshToken { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(18)]
    public string? Cnpj { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RefreshExpiresAt { get; set; }
}

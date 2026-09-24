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
    [MaxLength(128)]
    public string? UserId { get; set; }

    public MarketplaceType MarketplaceType { get; set; }

    [MaxLength(256)]
    public string? FriendlyName { get; set; }

    [MaxLength(128)]
    public string? ShopId { get; set; }

    string IShopScopedEntity.ShopId
    {
        get => ShopId ?? string.Empty;
        set => ShopId = value;
    }

    [MaxLength(256)]
    public string? ShopName { get; set; }

    /// <summary>ID de afiliado / Tracking ID da Shopee (ex.: 6512300000).</summary>
    [MaxLength(64)]
    public string? TrackingId { get; set; }

    /// <summary>Compatível com a coluna AffiliateTrackingId já persistida.</summary>
    [MaxLength(64)]
    public string? AffiliateTrackingId { get; set; }

    [MaxLength(256)]
    public string? AppKey { get; set; }

    [MaxLength(512)]
    public string? AppSecret { get; set; }

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(18)]
    public string? Cnpj { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RefreshExpiresAt { get; set; }
}

using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Database.Entity;

/// <summary>
/// Vínculo 1:N entre usuário e lojas de marketplace (TikTok Shop, Shopee, etc.).
/// Tokens persistidos com criptografia via AppDbContext.
/// </summary>
[Table("IntegracaoLoja")]
public class IntegracaoLoja : ITenantScopedEntity
{
    [Key]
    public int Id { get; set; }

    /// <summary>Proprietário da integração (Usuarios / UserAccount).</summary>
    public int UserId { get; set; }

    [JsonIgnore]
    public UserAccount? User { get; set; }

    public Guid TenantId { get; set; }

    [JsonIgnore]
    public Tenant? Tenant { get; set; }

    public MarketplaceType PlatformType { get; set; }

    [MaxLength(128)]
    public string? ShopId { get; set; }

    [MaxLength(256)]
    public string? FriendlyName { get; set; }

    /// <summary>ID de afiliado / Tracking ID (ex.: 18325850271).</summary>
    [MaxLength(64)]
    public string? AffiliateTrackingId { get; set; }

    [JsonIgnore]
    public string? AccessToken { get; set; }

    [JsonIgnore]
    public string? RefreshToken { get; set; }

    public DateTime ExpiresAt { get; set; }

    public MarketplaceIntegrationStatus Status { get; set; } = MarketplaceIntegrationStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public void Touch() => UpdatedAt = DateTime.UtcNow;
}

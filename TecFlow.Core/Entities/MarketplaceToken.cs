using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;
using TecFlow.Core.Enums;

namespace TecFlow.Core.Entities;

[Table("MarketplaceTokens")]
public class MarketplaceToken : BaseEntity, ITenantScopedEntity, IShopScopedEntity
{
    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(128)]
    public required string ShopId { get; set; }

    public MarketplaceType MarketplaceType { get; set; }

    [Required]
    public required string AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RefreshExpiresAt { get; set; }
}

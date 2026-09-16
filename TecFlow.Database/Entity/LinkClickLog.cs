using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Database.Entity;

/// <summary>Telemetria de geração e cliques em links de afiliado TecFlow.</summary>
[Table("LinkClickLog")]
public class LinkClickLog
{
    public const string EventKindGeneration = "Generation";
    public const string EventKindClick = "Click";

    [Key]
    public int Id { get; set; }

    /// <summary>UUID do link encurtado (ShortAffiliateLink.AffiliateLinkId).</summary>
    public Guid AffiliateLinkId { get; set; }

    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(128)]
    public string ShopId { get; set; } = string.Empty;

    [Required]
    [MaxLength(2048)]
    public string OriginalUrl { get; set; } = string.Empty;

    [Required]
    [MaxLength(2048)]
    public string ConvertedUrl { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string Platform { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(32)]
    public string EventKind { get; set; } = EventKindClick;

    [Required]
    [MaxLength(64)]
    public string IpAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string UserAgent { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string DeviceType { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? ReferrerUrl { get; set; }

    public ShortAffiliateLink? AffiliateLink { get; set; }

    public static string PlatformName(MarketplaceType platformType) => platformType switch
    {
        MarketplaceType.Shopee => "Shopee",
        MarketplaceType.TikTokShop => "TikTok Shop",
        _ => platformType.ToString()
    };
}

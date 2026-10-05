using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Enums;

namespace TecFlow.Core.Entities;

[Table("EvergreenOffers")]
public class EvergreenOffer : BaseEntity
{
    public int UserId { get; set; }

    [Required]
    [MaxLength(255)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ProductImageUrl { get; set; }

    [Required]
    [MaxLength(2048)]
    public string AffiliateUrl { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? OriginalUrl { get; set; }

    public MarketplaceType? PlatformType { get; set; }

    [MaxLength(64)]
    public string? PlatformName { get; set; }

    public int ClickCount { get; set; }

    public int ChampionScore { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public bool IsChampion { get; set; }
}

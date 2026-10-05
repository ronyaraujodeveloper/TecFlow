using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Enums;

namespace TecFlow.Core.Entities;

[Table("OfferRadarItems")]
public class OfferRadarItem : BaseEntity
{
    public int UserId { get; set; }

    [Required]
    [MaxLength(255)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ProductImageUrl { get; set; }

    [Required]
    [MaxLength(1000)]
    public string OriginalUrl { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? AffiliateUrl { get; set; }

    public MarketplaceType? PlatformType { get; set; }

    [MaxLength(64)]
    public string? PlatformName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ComparedPrice { get; set; }

    [MaxLength(32)]
    public string? CouponCode { get; set; }

    public int AttractivenessScore { get; set; }

    [Required]
    [MaxLength(32)]
    public string Source { get; set; } = OfferRadarSources.Mining;

    public bool IsAutoQueued { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}

public static class OfferRadarSources
{
    public const string Arbitrage = "Arbitrage";
    public const string PriceDrop = "PriceDrop";
    public const string Trend = "Trend";
    public const string Mining = "Mining";

    public static string ToUiLabel(string? source) => source switch
    {
        Arbitrage => "Menor preço",
        PriceDrop => "Queda de preço",
        Trend => "Em tendência",
        _ => "Mineração"
    };
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

[Table("GlobalTrendingDeals")]
public class GlobalTrendingDeal : BaseEntity
{
    [Required]
    [MaxLength(32)]
    public string Platform { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string PlatformProductId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ProductImageUrl { get; set; }

    [Required]
    [MaxLength(1000)]
    public string OriginalUrl { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PreviousPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PriceDropPercent { get; set; }

    public int EngagementCount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}

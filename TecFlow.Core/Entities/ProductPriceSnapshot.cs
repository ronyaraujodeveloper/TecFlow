using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Enums;

namespace TecFlow.Core.Entities;

[Table("ProductPriceSnapshots")]
public class ProductPriceSnapshot : BaseEntity
{
    public int UserId { get; set; }

    [Required]
    [MaxLength(160)]
    public string ProductKey { get; set; } = string.Empty;

    public MarketplaceType? PlatformType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [MaxLength(1000)]
    public string? SourceUrl { get; set; }

    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

[Table("ProductPriceHistory")]
public class ProductPriceHistory : BaseEntity
{
    [Required]
    [MaxLength(32)]
    public string Platform { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string PlatformProductId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [MaxLength(1000)]
    public string? SourceUrl { get; set; }

    [MaxLength(64)]
    public string? CouponCode { get; set; }

    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}

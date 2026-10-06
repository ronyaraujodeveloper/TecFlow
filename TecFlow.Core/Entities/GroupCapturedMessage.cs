using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Enums;

namespace TecFlow.Core.Entities;

[Table("GroupCapturedMessages")]
public class GroupCapturedMessage : BaseEntity
{
    public int UserId { get; set; }

    [Required]
    [MaxLength(16)]
    public string Channel { get; set; } = "WhatsApp";

    [Required]
    [MaxLength(160)]
    public string GroupKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string GroupName { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? ExternalMessageId { get; set; }

    [Required]
    public string RawText { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? MediaUrl { get; set; }

    [MaxLength(500)]
    public string? ProductImageUrl { get; set; }

    [Required]
    [MaxLength(1000)]
    public string OriginalUrl { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? ProductName { get; set; }

    [MaxLength(64)]
    public string? CouponCode { get; set; }

    [MaxLength(1000)]
    public string? PrimaryProductUrl { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ExtractedPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ValidatedPrice { get; set; }

    public MarketplaceType? PlatformType { get; set; }

    [MaxLength(64)]
    public string? PlatformName { get; set; }

    [Required]
    [MaxLength(32)]
    public string OfferStatus { get; set; } = GroupOfferStatuses.Verificando;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastValidatedAt { get; set; }

    public bool HasDirectProductUrl { get; set; }

    public bool IsIgnored { get; set; }

    public DateTime? IgnoredAt { get; set; }

    public bool IsAvailable { get; set; } = true;
}

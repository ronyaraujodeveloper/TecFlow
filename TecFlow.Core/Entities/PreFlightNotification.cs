using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

[Table("PreFlightNotifications")]
public class PreFlightNotification : BaseEntity
{
    public int UserId { get; set; }

    [Required]
    [MaxLength(16)]
    public string Channel { get; set; } = "WhatsApp";

    public int CampaignId { get; set; }

    [MaxLength(128)]
    public string? CampaignTitle { get; set; }

    [MaxLength(1000)]
    public string? ProductUrl { get; set; }

    [MaxLength(32)]
    public string? CouponCode { get; set; }

    [Required]
    [MaxLength(32)]
    public string AlertType { get; set; } = "Esgotado";

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? OriginalPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CurrentPrice { get; set; }

    [MaxLength(2048)]
    public string? SubstituteUrl { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? SubstitutePrice { get; set; }

    [MaxLength(255)]
    public string? SubstituteName { get; set; }

    public bool IsResolved { get; set; }

    public DateTime? ResolvedAt { get; set; }
}

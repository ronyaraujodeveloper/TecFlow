using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Entities;

namespace TecFlow.Database.Entity;

/// <summary>Telemetria de cliques em links encurtados TecFlow antes do redirect ao marketplace.</summary>
[Table("LinkClickLog")]
public class LinkClickLog
{
    [Key]
    public int Id { get; set; }

    /// <summary>UUID do link encurtado (ShortAffiliateLink.AffiliateLinkId).</summary>
    public Guid AffiliateLinkId { get; set; }

    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;

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
}

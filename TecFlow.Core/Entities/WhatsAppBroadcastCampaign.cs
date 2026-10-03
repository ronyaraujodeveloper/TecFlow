using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

[Table("WhatsAppBroadcastCampaigns")]
public class WhatsAppBroadcastCampaign : BaseEntity
{
    public int UserId { get; set; }

    [Required]
    [MaxLength(128)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string MessageText { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? ImageUrl { get; set; }

    [Required]
    public string TargetGroupJidsJson { get; set; } = "[]";

    public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;

    public int IntervalSeconds { get; set; } = WhatsAppBroadcastStatuses.DefaultIntervalSeconds;

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = WhatsAppBroadcastStatuses.Pending;
}

public static class WhatsAppBroadcastStatuses
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";

    public const int MinIntervalSeconds = 15;
    public const int MaxIntervalSeconds = 180;
    public const int DefaultIntervalSeconds = 30;
}

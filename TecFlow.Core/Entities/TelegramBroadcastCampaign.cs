using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

[Table("TelegramBroadcastCampaigns")]
public class TelegramBroadcastCampaign : BaseEntity
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
    [MaxLength(64)]
    public string TargetChatId { get; set; } = string.Empty;

    public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = TelegramBroadcastStatuses.Pending;
}

public static class TelegramBroadcastStatuses
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

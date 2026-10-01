using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

/// <summary>Sessão WhatsApp do afiliado orquestrada pela Evolution API (uma instância por UserId).</summary>
[Table("WhatsAppIntegrations")]
public class WhatsAppIntegration : BaseEntity
{
    public int UserId { get; set; }

    [Required]
    [MaxLength(128)]
    public string InstanceName { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string ConnectionStatus { get; set; } = WhatsAppConnectionStatuses.Disconnected;

    [MaxLength(32)]
    public string? PhoneNumber { get; set; }

    [MaxLength(128)]
    public string? ProfileName { get; set; }

    public DateTime? LastConnectedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public bool EnableAutoConvertBot { get; set; } = true;

    public bool ReplyToPrivateMessages { get; set; } = true;

    public bool ReplyToGroupMessages { get; set; } = false;
}

public static class WhatsAppConnectionStatuses
{
    public const string Disconnected = "Disconnected";
    public const string AwaitingQr = "AwaitingQr";
    public const string Connected = "Connected";
}

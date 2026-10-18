namespace Ofertio.Core.Entities;

/// <summary>Registro imutável de ação administrativa sensível do backoffice master.</summary>
public class MasterAuditLog : BaseEntity
{
    public int ActorUserId { get; set; }

    public string ActorEmail { get; set; } = string.Empty;

    public string ActorRole { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string? TargetId { get; set; }

    public string? Details { get; set; }

    public string? IpAddress { get; set; }
}

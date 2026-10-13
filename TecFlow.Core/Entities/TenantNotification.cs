namespace TecFlow.Core.Entities;

/// <summary>Alerta interno direcionado a um tenant (disparo por segmento).</summary>
public class TenantNotification : BaseEntity
{
    public Guid TenantId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Segment { get; set; } = string.Empty;

    public DateTime? ReadAt { get; set; }
}

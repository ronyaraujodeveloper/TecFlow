namespace TecFlow.Core.Entities;

/// <summary>Aviso global exibido no topo do painel de todos os usuários.</summary>
public class SystemBanner : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>info, warning ou maintenance.</summary>
    public string Severity { get; set; } = "info";

    public DateTime StartsAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndsAt { get; set; }

    public bool IsActive { get; set; } = true;

    public string? CreatedBy { get; set; }
}

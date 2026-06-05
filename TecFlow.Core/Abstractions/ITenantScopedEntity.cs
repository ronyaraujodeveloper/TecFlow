namespace TecFlow.Core.Abstractions;

/// <summary>Entidade com isolamento lógico por inquilino (Tenant).</summary>
public interface ITenantScopedEntity
{
    Guid TenantId { get; set; }
}

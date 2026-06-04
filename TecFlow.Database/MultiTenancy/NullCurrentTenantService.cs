namespace TecFlow.Database.MultiTenancy;

/// <summary>Implementação de design-time / testes sem contexto HTTP.</summary>
public sealed class NullCurrentTenantService : ICurrentTenantService
{
    public Guid? TenantId => null;

    public string? ShopId => null;

    public bool BypassTenantFilters { get; set; } = true;
}

using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Services;

public interface ITenantProvisioningService
{
    Task<Tenant> EnsureTenantForUserAsync(UserAccount user, CancellationToken cancellationToken = default);

    /// <summary>Garante um Tenant persistido em <c>dbo.Tenants</c> (cria "Tenant Principal" se a tabela estiver vazia).</summary>
    Task<Tenant> EnsurePersistedTenantAsync(Guid? preferredTenantId = null, CancellationToken cancellationToken = default);
}

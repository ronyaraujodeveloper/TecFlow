using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Services;

public interface ITenantProvisioningService
{
    Task<Tenant> EnsureTenantForUserAsync(UserAccount user, CancellationToken cancellationToken = default);
}

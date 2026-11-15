using Ofertio.Core.Entities;

namespace Ofertio.Business.Interfaces.Services;

public interface ITenantProvisioningService
{
    Task<Tenant> EnsureTenantForUserAsync(UserAccount user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cadastro novo: cria um Tenant próprio da conta, em Trial, com os dias definidos no plano Trial do Master.
    /// Sem plano Trial ativo, mantém o comportamento anterior (<see cref="EnsureTenantForUserAsync"/>).
    /// </summary>
    Task<Tenant> CreateTrialTenantForNewUserAsync(UserAccount user, CancellationToken cancellationToken = default);

    /// <summary>Garante um Tenant persistido em <c>dbo.Tenants</c> (cria "Tenant Principal" se a tabela estiver vazia).</summary>
    Task<Tenant> EnsurePersistedTenantAsync(Guid? preferredTenantId = null, CancellationToken cancellationToken = default);
}

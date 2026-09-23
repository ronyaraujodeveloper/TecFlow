using System.Globalization;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Mappings;
using TecFlow.Core.Entities;
using TecFlow.Database.Entity;

namespace TecFlow.Infrastructure.Services.Integrations;

/// <summary>Mapeia MarketplaceAccounts e garante TenantId/UserId válidos antes da persistência.</summary>
public sealed class MarketplaceAccountService
{
    private readonly ITenantProvisioningService _tenantProvisioning;
    private readonly IUserAccountRepository _userAccountRepository;

    public MarketplaceAccountService(
        ITenantProvisioningService tenantProvisioning,
        IUserAccountRepository userAccountRepository)
    {
        _tenantProvisioning = tenantProvisioning;
        _userAccountRepository = userAccountRepository;
    }

    public MarketplaceAccountDto MapToDto(MarketplaceAccount? account, IntegracaoLoja? integration = null) =>
        MarketplaceAccountMapper.ToDto(account, integration);

    public ConvertLinkResponseDto MapToConvertLinkResponse(MarketplaceAccount? account, IntegracaoLoja? integration = null) =>
        MarketplaceAccountMapper.ToConvertLinkResponse(account, integration);

    public async Task PrepareForPersistAsync(
        MarketplaceAccount account,
        UserAccount? owner,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (owner is null)
        {
            owner = await ResolveValidUserAsync(account.UserId, cancellationToken);
        }

        Tenant tenant;
        if (owner is not null)
        {
            tenant = await _tenantProvisioning.EnsureTenantForUserAsync(owner, cancellationToken);
            account.UserId = owner.Id.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            var preferred = account.TenantId == Guid.Empty ? (Guid?)null : account.TenantId;
            tenant = await _tenantProvisioning.EnsurePersistedTenantAsync(preferred, cancellationToken);
        }

        account.TenantId = tenant.Id;
    }

    private async Task<UserAccount?> ResolveValidUserAsync(string? userId, CancellationToken cancellationToken)
    {
        if (int.TryParse(userId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
        {
            var byId = await _userAccountRepository.GetByIdIgnoringFiltersAsync(parsed)
                ?? await _userAccountRepository.GetByIdAsync(parsed);
            if (byId is not null)
            {
                return byId;
            }
        }

        return await _userAccountRepository.GetFirstIgnoringFiltersAsync()
            ?? await _userAccountRepository.GetByIdIgnoringFiltersAsync(1)
            ?? await _userAccountRepository.GetByIdAsync(1);
    }
}

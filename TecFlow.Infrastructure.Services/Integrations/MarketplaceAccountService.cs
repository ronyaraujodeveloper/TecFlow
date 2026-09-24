using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Mappings;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Database.Entity;

namespace TecFlow.Infrastructure.Services.Integrations;

/// <summary>Mapeia MarketplaceAccounts e garante TenantId/UserId válidos antes da persistência.</summary>
public sealed class MarketplaceAccountService
{
    private readonly ITenantProvisioningService _tenantProvisioning;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IMarketplaceAccountRepository _marketplaceAccountRepository;
    private readonly AppDbContext _context;
    private readonly ILogger<MarketplaceAccountService> _logger;

    public MarketplaceAccountService(
        ITenantProvisioningService tenantProvisioning,
        IUserAccountRepository userAccountRepository,
        IMarketplaceAccountRepository marketplaceAccountRepository,
        AppDbContext context,
        ILogger<MarketplaceAccountService> logger)
    {
        _tenantProvisioning = tenantProvisioning;
        _userAccountRepository = userAccountRepository;
        _marketplaceAccountRepository = marketplaceAccountRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<bool> InativarContaAsync(int accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"[MarketplaceAccountService] InativarContaAsync accountId={accountId}");
            _logger.LogInformation("InativarContaAsync accountId={AccountId}", accountId);
            if (accountId <= 0)
            {
                Console.WriteLine("[MarketplaceAccountService] InativarContaAsync ignorado: accountId inválido.");
                return false;
            }

            var account = await _context.MarketplaceAccounts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(item => item.Id == accountId, cancellationToken);
            if (account is null)
            {
                Console.WriteLine($"[MarketplaceAccountService] Conta {accountId} não encontrada no AppDbContext.");
                _logger.LogWarning("InativarContaAsync: MarketplaceAccount {AccountId} não encontrada.", accountId);
                return false;
            }

            account.IsActive = false;
            account.Touch();
            await _context.SaveChangesAsync(cancellationToken);
            Console.WriteLine($"[MarketplaceAccountService] SaveChangesAsync IsActive=false accountId={accountId}");
            _logger.LogInformation("Conta {AccountId} inativada no SQL Server.", accountId);
            return true;
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"[ERRO EF] InativarContaAsync accountId={accountId}: {ex.Message} - {ex.InnerException?.Message} - {ex.StackTrace}");
            _logger.LogError(ex, "Erro de validação/persistência ao inativar MarketplaceAccount {AccountId}.", accountId);
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO] InativarContaAsync accountId={accountId}: {ex.Message} - {ex.StackTrace}");
            _logger.LogError(ex, "Erro ao inativar MarketplaceAccount {AccountId}.", accountId);
            throw;
        }
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

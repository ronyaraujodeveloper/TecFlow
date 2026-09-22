using System.Globalization;
using Microsoft.Extensions.Hosting;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;
using TecFlow.Database.Filter;
using TecFlow.Database.Pagin;
using TecFlow.Infrastructure.Services.Integrations.Auth;

namespace TecFlow.Infrastructure.Services.Integrations;

public class IntegracaoLojaService : IIntegracaoLojaService
{
    private readonly IIntegracaoLojaRepository _integracaoLojaRepository;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IMarketplaceAccountRepository _marketplaceAccountRepository;
    private readonly IMarketplaceAuthService _marketplaceAuthService;
    private readonly IHostEnvironment _hostEnvironment;

    public IntegracaoLojaService(
        IIntegracaoLojaRepository integracaoLojaRepository,
        IUserAccountRepository userAccountRepository,
        IMarketplaceAccountRepository marketplaceAccountRepository,
        IMarketplaceAuthService marketplaceAuthService,
        IHostEnvironment hostEnvironment)
    {
        _integracaoLojaRepository = integracaoLojaRepository;
        _userAccountRepository = userAccountRepository;
        _marketplaceAccountRepository = marketplaceAccountRepository;
        _marketplaceAuthService = marketplaceAuthService;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<IntegracaoLojaResponseDto> ListByUserAsync(
        int userId,
        IntegracaoLojaFilter filter,
        CancellationToken cancellationToken = default)
    {
        filter.UserId = userId;
        var userKey = userId.ToString(CultureInfo.InvariantCulture);
        var accounts = await _marketplaceAccountRepository.ListByUserIdAsync(userKey, cancellationToken);
        var integrations = await _integracaoLojaRepository.ListByUserIdAsync(userId, cancellationToken);

        var dtos = accounts
            .Select(account =>
            {
                var integration = integrations.FirstOrDefault(item =>
                    item.ShopId == account.ShopId && item.PlatformType == account.MarketplaceType);
                return ToAccountDto(account, integration);
            })
            .ToList();

        foreach (var integration in integrations)
        {
            var alreadyListed = dtos.Any(item =>
                item.ShopId == integration.ShopId && item.PlatformType == integration.PlatformType);
            if (!alreadyListed)
            {
                dtos.Add(ToAccountDto(integration));
            }
        }

        if (filter.PlatformType.HasValue)
        {
            dtos = dtos.Where(item => item.PlatformType == filter.PlatformType.Value).ToList();
        }

        dtos = dtos.OrderByDescending(item => item.CreatedAt).ToList();
        var (pageItems, meta) = PagedListHelper.Slice(dtos, filter);

        return new IntegracaoLojaResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = pageItems.ToList(),
            Paging = PagingInfoDto.FromMeta(meta)
        };
    }

    public async Task<IntegracaoLojaResponseDto> LinkAsync(
        int userId,
        IntegracaoLojaDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto is null)
        {
            return Fail("Payload de vinculação inválido.");
        }

        ApplyHomologFallbacks(dto);

        if (dto.PlatformType == MarketplaceType.Shopee)
        {
            return await LinkShopeeUniversalAccountAsync(userId, dto, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(dto.AuthorizationCode))
        {
            return Fail("Código de autorização OAuth é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(dto.ShopId))
        {
            return Fail("ShopId é obrigatório.");
        }

        dto.AuthorizationCode = dto.AuthorizationCode.Trim();
        dto.ShopId = dto.ShopId.Trim();

        if (string.IsNullOrWhiteSpace(dto.FriendlyName))
        {
            return Fail("Nome amigável da loja é obrigatório.");
        }

        var user = await _userAccountRepository.GetByIdAsync(userId);
        if (user is null)
        {
            return Fail("Usuário não encontrado.");
        }

        var oauthResult = await _marketplaceAuthService.CallbackAndGenerateTokensAsync(
            dto.PlatformType,
            dto.AuthorizationCode.Trim(),
            dto.ShopId.Trim(),
            cancellationToken,
            userId.ToString(CultureInfo.InvariantCulture));

        if (!oauthResult.Success)
        {
            return Fail(oauthResult.Descricao);
        }

        var marketplaceAccount = await _marketplaceAccountRepository.GetByShopAsync(
            dto.ShopId.Trim(),
            dto.PlatformType);

        if (marketplaceAccount is null)
        {
            return Fail("Tokens OAuth gerados, mas não foi possível localizar a conta marketplace persistida.");
        }

        marketplaceAccount.UserId = userId.ToString(CultureInfo.InvariantCulture);
        marketplaceAccount.FriendlyName = dto.FriendlyName.Trim();
        marketplaceAccount.ShopName = dto.FriendlyName.Trim();
        marketplaceAccount.IsActive = true;
        try
        {
            await _marketplaceAccountRepository.UpsertAsync(marketplaceAccount);
        }
        catch (Exception ex)
        {
            return Fail(MarketplaceAuthService.FormatSqlError(ex));
        }

        var existing = await _integracaoLojaRepository.GetByUserShopPlatformAsync(
            userId,
            dto.ShopId.Trim(),
            dto.PlatformType,
            cancellationToken);

        var status = ResolveStatus(marketplaceAccount.ExpiresAt, MarketplaceIntegrationStatus.Active);

        if (existing is not null)
        {
            existing.FriendlyName = dto.FriendlyName.Trim();
            existing.AccessToken = marketplaceAccount.AccessToken;
            existing.RefreshToken = marketplaceAccount.RefreshToken;
            existing.ExpiresAt = marketplaceAccount.ExpiresAt;
            existing.Status = status;
            existing.Touch();
            await _integracaoLojaRepository.UpdateAsync(existing, cancellationToken);

            return new IntegracaoLojaResponseDto
            {
                Status = true,
                Descricao = ResolveLinkSuccessMessage(dto, "Integração atualizada com sucesso."),
                Data = ToAccountDto(marketplaceAccount, existing)
            };
        }

        var integration = new IntegracaoLoja
        {
            UserId = userId,
            TenantId = user.TenantId,
            PlatformType = dto.PlatformType,
            ShopId = dto.ShopId.Trim(),
            FriendlyName = dto.FriendlyName.Trim(),
            AccessToken = marketplaceAccount.AccessToken,
            RefreshToken = marketplaceAccount.RefreshToken,
            ExpiresAt = marketplaceAccount.ExpiresAt,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        await _integracaoLojaRepository.AddAsync(integration, cancellationToken);

        return new IntegracaoLojaResponseDto
        {
            Status = true,
            Descricao = ResolveLinkSuccessMessage(dto, "Loja vinculada com sucesso."),
            Data = ToAccountDto(marketplaceAccount, integration)
        };
    }

    public async Task<IntegracaoLojaResponseDto> UnlinkAsync(
        int userId,
        int integrationId,
        CancellationToken cancellationToken = default)
    {
        var userKey = userId.ToString(CultureInfo.InvariantCulture);
        var integration = await _integracaoLojaRepository.GetByIdAsync(integrationId, cancellationToken);
        if (integration is not null && integration.UserId != userId)
        {
            integration = null;
        }

        var account = await _marketplaceAccountRepository.GetByIdAsync(integrationId, cancellationToken);
        if (account is not null && !string.Equals(account.UserId, userKey, StringComparison.Ordinal))
        {
            account = null;
        }

        if (integration is null && account is null)
        {
            return Fail("Integração não encontrada para o usuário autenticado.");
        }

        if (account is null && integration is not null)
        {
            account = await _marketplaceAccountRepository.GetByShopAsync(integration.ShopId, integration.PlatformType);
        }

        if (account is not null)
        {
            account.IsActive = false;
            account.Touch();
            await _marketplaceAccountRepository.UpsertAsync(account);
        }

        if (integration is null && account is not null)
        {
            integration = await _integracaoLojaRepository.GetByUserShopPlatformAsync(
                userId,
                account.ShopId,
                account.MarketplaceType,
                cancellationToken);
        }

        if (integration is not null)
        {
            integration.Status = MarketplaceIntegrationStatus.Inactive;
            integration.Touch();
            await _integracaoLojaRepository.UpdateAsync(integration, cancellationToken);
            await _integracaoLojaRepository.DeleteAsync(integration.Id, cancellationToken);
        }

        return new IntegracaoLojaResponseDto
        {
            Status = true,
            Descricao = "Loja desvinculada com sucesso."
        };
    }

    private static IntegracaoLoja SyncStatus(IntegracaoLoja item)
    {
        if (item.Status == MarketplaceIntegrationStatus.Inactive)
        {
            return item;
        }

        item.Status = ResolveStatus(item.ExpiresAt, item.Status);
        return item;
    }

    private static MarketplaceIntegrationStatus ResolveStatus(
        DateTime expiresAt,
        MarketplaceIntegrationStatus currentStatus)
    {
        if (currentStatus == MarketplaceIntegrationStatus.Inactive)
        {
            return MarketplaceIntegrationStatus.Inactive;
        }

        return expiresAt <= DateTime.UtcNow
            ? MarketplaceIntegrationStatus.Expired
            : MarketplaceIntegrationStatus.Active;
    }

    private static MarketplaceAccountDto ToAccountDto(IntegracaoLoja item)
    {
        item = SyncStatus(item);
        return new MarketplaceAccountDto
        {
            Id = item.Id,
            UserId = item.UserId,
            TenantId = item.TenantId,
            ShopId = item.ShopId,
            FriendlyName = item.FriendlyName,
            ShopName = string.IsNullOrWhiteSpace(item.FriendlyName) ? item.ShopId : item.FriendlyName,
            PlatformType = item.PlatformType,
            ExpiresAt = item.ExpiresAt,
            Status = item.Status,
            CreatedAt = item.CreatedAt
        };
    }

    private static MarketplaceAccountDto ToAccountDto(MarketplaceAccount account, IntegracaoLoja? integration)
    {
        _ = int.TryParse(account.UserId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedUserId);
        var status = account.IsActive
            ? ResolveStatus(account.ExpiresAt, MarketplaceIntegrationStatus.Active)
            : MarketplaceIntegrationStatus.Inactive;

        return new MarketplaceAccountDto
        {
            Id = integration?.Id ?? account.Id,
            UserId = parsedUserId,
            TenantId = account.TenantId,
            ShopId = account.ShopId,
            FriendlyName = string.IsNullOrWhiteSpace(account.FriendlyName) ? account.ShopName : account.FriendlyName,
            ShopName = string.IsNullOrWhiteSpace(account.ShopName) ? account.ShopId : account.ShopName,
            PlatformType = account.MarketplaceType,
            ExpiresAt = account.ExpiresAt,
            Status = status,
            CreatedAt = account.CreatedAt
        };
    }

    private void ApplyHomologFallbacks(IntegracaoLojaDto dto)
    {
        if (!AllowsHomologFallbacks())
        {
            return;
        }

        if (dto.PlatformType == 0)
        {
            dto.PlatformType = MarketplaceType.Shopee;
        }

        if (dto.PlatformType == MarketplaceType.Shopee)
        {
            if (string.IsNullOrWhiteSpace(dto.FriendlyName))
            {
                dto.FriendlyName = "Loja Homolog";
            }

            return;
        }

        var code = dto.AuthorizationCode?.Trim() ?? string.Empty;
        var shop = dto.ShopId?.Trim() ?? string.Empty;
        var shopIsNumeric = long.TryParse(shop, NumberStyles.Integer, CultureInfo.InvariantCulture, out var shopId)
            && shopId > 0;
        var codeIsNumeric = long.TryParse(code, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
        var shopLooksLikeStub = HomologMarketplaceAuth.IsStubAuthorizationCode(shop);

        if (!shopIsNumeric && shopLooksLikeStub && codeIsNumeric)
        {
            (code, shop) = (shop, code);
            shopIsNumeric = true;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            code = HomologMarketplaceAuth.StubAuthorizationCode;
        }

        if (!shopIsNumeric)
        {
            shop = "123456";
        }

        dto.AuthorizationCode = code;
        dto.ShopId = shop;
    }

    private async Task<IntegracaoLojaResponseDto> LinkShopeeUniversalAccountAsync(
        int userId,
        IntegracaoLojaDto dto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.FriendlyName))
        {
            return Fail("Informe um apelido / nome amigável para a conta Shopee.");
        }

        var user = await _userAccountRepository.GetByIdAsync(userId);
        if (user is null)
        {
            return Fail("Usuário não encontrado.");
        }

        var trackingId = FirstNonEmpty(dto.TrackingId, dto.ShopId);
        var shopKey = string.IsNullOrWhiteSpace(trackingId)
            ? $"ul-{userId}-{Slug(dto.FriendlyName)}"
            : trackingId;
        dto.ShopId = shopKey;

        var expiresAt = DateTime.UtcNow.AddYears(10);
        var placeholderToken = HomologMarketplaceAuth.StubAccessToken;

        var marketplaceAccount = await _marketplaceAccountRepository.GetByShopAsync(shopKey, MarketplaceType.Shopee)
            ?? new MarketplaceAccount
            {
                TenantId = user.TenantId,
                UserId = userId.ToString(CultureInfo.InvariantCulture),
                ShopId = shopKey,
                MarketplaceType = MarketplaceType.Shopee
            };

        marketplaceAccount.UserId = userId.ToString(CultureInfo.InvariantCulture);
        marketplaceAccount.TenantId = user.TenantId;
        marketplaceAccount.FriendlyName = dto.FriendlyName.Trim();
        marketplaceAccount.ShopName = dto.FriendlyName.Trim();
        marketplaceAccount.ShopId = shopKey;
        marketplaceAccount.AccessToken = string.IsNullOrWhiteSpace(marketplaceAccount.AccessToken)
            ? placeholderToken
            : marketplaceAccount.AccessToken;
        marketplaceAccount.ExpiresAt = expiresAt;
        marketplaceAccount.IsActive = true;
        marketplaceAccount.MarketplaceType = MarketplaceType.Shopee;
        marketplaceAccount.Touch();

        try
        {
            await _marketplaceAccountRepository.UpsertAsync(marketplaceAccount);
        }
        catch (Exception ex)
        {
            return Fail(MarketplaceAuthService.FormatSqlError(ex));
        }

        var existing = await _integracaoLojaRepository.GetByUserShopPlatformAsync(
            userId,
            shopKey,
            MarketplaceType.Shopee,
            cancellationToken);

        if (existing is not null)
        {
            existing.FriendlyName = dto.FriendlyName.Trim();
            existing.AccessToken = placeholderToken;
            existing.ExpiresAt = expiresAt;
            existing.Status = MarketplaceIntegrationStatus.Active;
            existing.Touch();
            await _integracaoLojaRepository.UpdateAsync(existing, cancellationToken);

            return new IntegracaoLojaResponseDto
            {
                Status = true,
                Descricao = "Conta Shopee vinculada no modo Universal Link.",
                Data = ToAccountDto(marketplaceAccount, existing)
            };
        }

        var integration = new IntegracaoLoja
        {
            UserId = userId,
            TenantId = user.TenantId,
            PlatformType = MarketplaceType.Shopee,
            ShopId = shopKey,
            FriendlyName = dto.FriendlyName.Trim(),
            AccessToken = placeholderToken,
            ExpiresAt = expiresAt,
            Status = MarketplaceIntegrationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _integracaoLojaRepository.AddAsync(integration, cancellationToken);

        return new IntegracaoLojaResponseDto
        {
            Status = true,
            Descricao = "Conta Shopee vinculada no modo Universal Link.",
            Data = ToAccountDto(marketplaceAccount, integration)
        };
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private static string Slug(string value)
    {
        var chars = value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        var slug = new string(chars).Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
        {
            return "conta";
        }

        return slug.Length <= 80 ? slug : slug[..80];
    }

    private string ResolveLinkSuccessMessage(IntegracaoLojaDto dto, string fallback) =>
        HomologMarketplaceAuth.ShouldSkipRemoteOAuth(_hostEnvironment.EnvironmentName, dto.AuthorizationCode)
            ? HomologMarketplaceAuth.ManualLinkSuccessMessage
            : fallback;

    private bool AllowsHomologFallbacks() =>
        _hostEnvironment.IsDevelopment()
        || _hostEnvironment.IsEnvironment("Homologacao");

    private static IntegracaoLojaResponseDto Fail(string message) =>
        new()
        {
            Status = false,
            Descricao = message
        };
}

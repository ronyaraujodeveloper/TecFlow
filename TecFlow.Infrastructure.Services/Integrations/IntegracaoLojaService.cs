using System.Globalization;
using Microsoft.Extensions.Hosting;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Mappings;
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
    private readonly MarketplaceAccountService _marketplaceAccountService;
    private readonly IHostEnvironment _hostEnvironment;

    public IntegracaoLojaService(
        IIntegracaoLojaRepository integracaoLojaRepository,
        IUserAccountRepository userAccountRepository,
        IMarketplaceAccountRepository marketplaceAccountRepository,
        IMarketplaceAuthService marketplaceAuthService,
        MarketplaceAccountService marketplaceAccountService,
        IHostEnvironment hostEnvironment)
    {
        _integracaoLojaRepository = integracaoLojaRepository;
        _userAccountRepository = userAccountRepository;
        _marketplaceAccountRepository = marketplaceAccountRepository;
        _marketplaceAuthService = marketplaceAuthService;
        _marketplaceAccountService = marketplaceAccountService;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<IntegracaoLojaResponseDto> ListByUserAsync(
        int userId,
        IntegracaoLojaFilter filter,
        CancellationToken cancellationToken = default)
    {
        var owner = await ResolvePersistableUserAsync(userId);
        var persistUserId = owner?.Id ?? userId;
        filter.UserId = persistUserId;
        var userKey = persistUserId.ToString(CultureInfo.InvariantCulture);

        try
        {
            var accounts = await _marketplaceAccountRepository.ListByUserIdAsync(userKey, cancellationToken);
            var integrations = await _integracaoLojaRepository.ListByUserIdAsync(persistUserId, cancellationToken);

            var dtos = new List<MarketplaceAccountDto>();
            foreach (var account in accounts)
            {
                try
                {
                    var integration = integrations.FirstOrDefault(item =>
                        item.ShopId == account.ShopId && item.PlatformType == account.MarketplaceType);
                    dtos.Add(MarketplaceAccountMapper.ToDto(account, integration));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERRO MAP MarketplaceAccount]: {ex.Message} - {ex.StackTrace}");
                }
            }

            foreach (var integration in integrations)
            {
                var alreadyListed = dtos.Any(item =>
                    item.ShopId == integration.ShopId && item.PlatformType == integration.PlatformType);
                if (!alreadyListed)
                {
                    dtos.Add(MarketplaceAccountMapper.ToDto(integration));
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
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO SQL MarketplaceAccounts]: {ex.Message} - {ex.StackTrace}");
            return Fail($"Erro do Servidor/SQL: {ex.Message}");
        }
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

        var user = await ResolvePersistableUserAsync(userId);
        if (user is null)
        {
            return Fail("Usuário não encontrado.");
        }

        var persistUserId = user.Id;

        if (dto.PlatformType is MarketplaceType.Shopee or MarketplaceType.TikTokShop)
        {
            return await LinkUniversalAccountAsync(user, dto, cancellationToken);
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

        var oauthResult = await _marketplaceAuthService.CallbackAndGenerateTokensAsync(
            dto.PlatformType,
            dto.AuthorizationCode.Trim(),
            dto.ShopId.Trim(),
            cancellationToken,
            persistUserId.ToString(CultureInfo.InvariantCulture));

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

        marketplaceAccount.UserId = persistUserId.ToString(CultureInfo.InvariantCulture);
        marketplaceAccount.FriendlyName = dto.FriendlyName.Trim();
        marketplaceAccount.ShopName = dto.FriendlyName.Trim();
        marketplaceAccount.IsActive = true;
        if (!string.IsNullOrWhiteSpace(dto.TrackingId))
        {
            marketplaceAccount.AffiliateTrackingId = dto.TrackingId.Trim();
            marketplaceAccount.TrackingId = marketplaceAccount.AffiliateTrackingId;
        }

        await _marketplaceAccountService.PrepareForPersistAsync(marketplaceAccount, user, cancellationToken);
        user.TenantId = marketplaceAccount.TenantId;
        try
        {
            await _marketplaceAccountRepository.UpsertAsync(marketplaceAccount);
        }
        catch (Exception ex)
        {
            return Fail(MarketplaceAuthService.FormatSqlError(ex));
        }

        var existing = await _integracaoLojaRepository.GetByUserShopPlatformAsync(
            persistUserId,
            dto.ShopId.Trim(),
            dto.PlatformType,
            cancellationToken);

        var status = ResolveStatus(marketplaceAccount.ExpiresAt, MarketplaceIntegrationStatus.Active);

        if (existing is not null)
        {
            existing.FriendlyName = dto.FriendlyName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.TrackingId))
            {
                existing.AffiliateTrackingId = dto.TrackingId.Trim();
            }
            existing.AccessToken = marketplaceAccount.AccessToken ?? string.Empty;
            existing.RefreshToken = marketplaceAccount.RefreshToken;
            existing.ExpiresAt = marketplaceAccount.ExpiresAt;
            existing.Status = status;
            existing.Touch();
            await _integracaoLojaRepository.UpdateAsync(existing, cancellationToken);

            return new IntegracaoLojaResponseDto
            {
                Status = true,
                Descricao = ResolveLinkSuccessMessage(dto, "Integração atualizada com sucesso."),
                Data = MarketplaceAccountMapper.ToDto(marketplaceAccount, existing)
            };
        }

        var integration = new IntegracaoLoja
        {
            UserId = persistUserId,
            TenantId = marketplaceAccount.TenantId,
            PlatformType = dto.PlatformType,
            ShopId = dto.ShopId.Trim(),
            FriendlyName = dto.FriendlyName.Trim(),
            AffiliateTrackingId = string.IsNullOrWhiteSpace(dto.TrackingId) ? null : dto.TrackingId.Trim(),
            AccessToken = marketplaceAccount.AccessToken ?? string.Empty,
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
            Data = MarketplaceAccountMapper.ToDto(marketplaceAccount, integration)
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
            account = await _marketplaceAccountRepository.GetByShopAsync(
                integration.ShopId ?? string.Empty,
                integration.PlatformType);
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
                account.ShopId ?? string.Empty,
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

        if (dto.PlatformType is MarketplaceType.Shopee or MarketplaceType.TikTokShop)
        {
            if (string.IsNullOrWhiteSpace(dto.FriendlyName))
            {
                dto.FriendlyName = dto.PlatformType == MarketplaceType.TikTokShop
                    ? "Loja TikTok Homolog"
                    : "Loja Homolog";
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

    private async Task<IntegracaoLojaResponseDto> LinkUniversalAccountAsync(
        UserAccount user,
        IntegracaoLojaDto dto,
        CancellationToken cancellationToken)
    {
        var platform = dto.PlatformType;
        var platformLabel = platform == MarketplaceType.TikTokShop ? "TikTok Shop" : "Shopee";
        if (string.IsNullOrWhiteSpace(dto.FriendlyName))
        {
            return Fail($"Informe um apelido / nome amigável para a conta {platformLabel}.");
        }

        var persistUserId = user.Id;
        var persistUserKey = persistUserId.ToString(CultureInfo.InvariantCulture);
        var affiliateTrackingId = FirstNonEmpty(dto.TrackingId);
        var shopPrefix = platform == MarketplaceType.TikTokShop ? "ul-tt" : "ul";
        var shopKey = $"{shopPrefix}-{persistUserId}-{Slug(dto.FriendlyName)}";
        dto.ShopId = shopKey;

        var expiresAt = DateTime.UtcNow.AddYears(10);
        var placeholderToken = HomologMarketplaceAuth.StubAccessToken;

        var marketplaceAccount = await _marketplaceAccountRepository.GetByShopAsync(shopKey, platform)
            ?? new MarketplaceAccount
            {
                TenantId = user.TenantId,
                UserId = persistUserKey,
                ShopId = shopKey,
                MarketplaceType = platform
            };

        marketplaceAccount.UserId = persistUserKey;
        marketplaceAccount.TenantId = user.TenantId;
        marketplaceAccount.FriendlyName = dto.FriendlyName.Trim();
        marketplaceAccount.ShopName = dto.FriendlyName.Trim();
        marketplaceAccount.ShopId = shopKey;
        marketplaceAccount.AffiliateTrackingId = string.IsNullOrWhiteSpace(affiliateTrackingId)
            ? marketplaceAccount.AffiliateTrackingId
            : affiliateTrackingId;
        marketplaceAccount.TrackingId = marketplaceAccount.AffiliateTrackingId;
        marketplaceAccount.AccessToken = string.IsNullOrWhiteSpace(marketplaceAccount.AccessToken)
            ? placeholderToken
            : marketplaceAccount.AccessToken;
        marketplaceAccount.ExpiresAt = expiresAt;
        marketplaceAccount.IsActive = true;
        marketplaceAccount.MarketplaceType = platform;
        marketplaceAccount.Touch();

        await _marketplaceAccountService.PrepareForPersistAsync(marketplaceAccount, user, cancellationToken);
        user.TenantId = marketplaceAccount.TenantId;

        try
        {
            await _marketplaceAccountRepository.UpsertAsync(marketplaceAccount);
        }
        catch (Exception ex)
        {
            return Fail(MarketplaceAuthService.FormatSqlError(ex));
        }

        var existing = await _integracaoLojaRepository.GetByUserShopPlatformAsync(
            persistUserId,
            shopKey,
            platform,
            cancellationToken);

        if (existing is not null)
        {
            existing.FriendlyName = dto.FriendlyName.Trim();
            existing.AffiliateTrackingId = string.IsNullOrWhiteSpace(affiliateTrackingId)
                ? existing.AffiliateTrackingId
                : affiliateTrackingId;
            existing.AccessToken = placeholderToken;
            existing.ExpiresAt = expiresAt;
            existing.Status = MarketplaceIntegrationStatus.Active;
            existing.Touch();
            await _integracaoLojaRepository.UpdateAsync(existing, cancellationToken);

            return new IntegracaoLojaResponseDto
            {
                Status = true,
                Descricao = $"Conta {platformLabel} vinculada no modo Universal Link.",
                Data = MarketplaceAccountMapper.ToDto(marketplaceAccount, existing)
            };
        }

        var integration = new IntegracaoLoja
        {
            UserId = persistUserId,
            TenantId = marketplaceAccount.TenantId,
            PlatformType = platform,
            ShopId = shopKey,
            FriendlyName = dto.FriendlyName.Trim(),
            AffiliateTrackingId = affiliateTrackingId,
            AccessToken = placeholderToken,
            ExpiresAt = expiresAt,
            Status = MarketplaceIntegrationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _integracaoLojaRepository.AddAsync(integration, cancellationToken);

        return new IntegracaoLojaResponseDto
        {
            Status = true,
            Descricao = $"Conta {platformLabel} vinculada no modo Universal Link.",
            Data = MarketplaceAccountMapper.ToDto(marketplaceAccount, integration)
        };
    }

    private async Task<UserAccount?> ResolvePersistableUserAsync(int claimedUserId)
    {
        if (claimedUserId > 0)
        {
            var claimed = await _userAccountRepository.GetByIdIgnoringFiltersAsync(claimedUserId)
                ?? await _userAccountRepository.GetByIdAsync(claimedUserId);
            if (claimed is not null)
            {
                return claimed;
            }
        }

        var fallback = await _userAccountRepository.GetByIdIgnoringFiltersAsync(HomologMarketplaceAuth.FallbackUserId)
            ?? await _userAccountRepository.GetByIdAsync(HomologMarketplaceAuth.FallbackUserId);
        if (fallback is not null)
        {
            return fallback;
        }

        var first = await _userAccountRepository.GetFirstIgnoringFiltersAsync();
        if (first is not null)
        {
            return first;
        }

        foreach (var email in new[] { "demo@tecso.local", "demo@TecFlow.local" })
        {
            var byEmail = await _userAccountRepository.GetByEmailAsync(email);
            if (byEmail is not null)
            {
                return byEmail;
            }
        }

        try
        {
            return await _userAccountRepository.CreateAsync(new UserAccount
            {
                Name = "Homolog",
                Email = "demo@tecso.local",
                PasswordHash = "homolog-placeholder-hash",
                Plan = "Free",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch
        {
            return await _userAccountRepository.GetByEmailAsync("demo@tecso.local")
                ?? await _userAccountRepository.GetFirstIgnoringFiltersAsync();
        }
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

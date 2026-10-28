using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Integrations.TikTokShop;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Master;

/// <summary>Tokens Master das APIs oficiais (Mercado Livre e TikTok Shop) e configurações globais das integrações.</summary>
public sealed class GlobalIntegrationService : IGlobalIntegrationService
{
    private const string TargetType = "GlobalIntegrationToken";
    private const string ProviderKey = "mercado-livre";

    private readonly AppDbContext _context;
    private readonly IMarketplaceAuthService _marketplaceAuth;
    private readonly ISystemSettingsService _settings;
    private readonly IMasterAuditService _audit;
    private readonly IOptionsMonitor<MercadoLivreIntegrationOptions> _options;
    private readonly IOptionsMonitor<TikTokShopIntegrationOptions>? _tikTokOptions;
    private readonly ILogger<GlobalIntegrationService> _logger;

    public GlobalIntegrationService(
        AppDbContext context,
        IMarketplaceAuthService marketplaceAuth,
        ISystemSettingsService settings,
        IMasterAuditService audit,
        IOptionsMonitor<MercadoLivreIntegrationOptions> options,
        ILogger<GlobalIntegrationService> logger,
        IOptionsMonitor<TikTokShopIntegrationOptions>? tikTokOptions = null)
    {
        _context = context;
        _marketplaceAuth = marketplaceAuth;
        _settings = settings;
        _audit = audit;
        _options = options;
        _tikTokOptions = tikTokOptions;
        _logger = logger;
    }

    private TikTokShopIntegrationOptions TikTok => _tikTokOptions?.CurrentValue ?? new TikTokShopIntegrationOptions();

    public async Task<GlobalIntegrationStatusDto> GetMercadoLivreStatusAsync(CancellationToken cancellationToken = default)
    {
        var token = await _context.GlobalIntegrationTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Provider == MarketplaceType.MercadoLivre, cancellationToken);
        var now = DateTime.UtcNow;
        var options = _options.CurrentValue;
        var status = GlobalIntegrationRules.ResolveStatus(token, now);
        return new GlobalIntegrationStatusDto
        {
            Provider = ProviderKey,
            ProviderLabel = "Mercado Livre",
            Status = status,
            ExpiresAt = token?.ExpiresAt,
            RenewsOnNextCall = status == GlobalIntegrationRules.StatusActive && !GlobalIntegrationRules.IsAccessTokenFresh(token, now),
            RefreshExpiresAt = token?.RefreshExpiresAt,
            LastRefreshAt = token?.LastRefreshAt,
            LastError = token?.LastError,
            AuthorizedBy = token?.AuthorizedBy,
            AuthorizedAt = token?.AuthorizedAt,
            HasAppCredentials = options.HasAppCredentials,
            SearchCacheMinutes = options.ResolveSearchCacheMinutes()
        };
    }

    public async Task<MasterOperationResultDto> SaveMercadoLivreSettingsAsync(
        MasterActor actor,
        GlobalIntegrationSettingsDto request,
        CancellationToken cancellationToken = default)
    {
        var minutes = request.SearchCacheMinutes;
        if (minutes < 0 || minutes > MercadoLivreSearchCacheRules.MaxMinutes)
        {
            return Fail($"Informe um tempo de cache entre 0 e {MercadoLivreSearchCacheRules.MaxMinutes} minutos (0 desliga o cache).");
        }

        var saved = await _settings.SaveAsync(
            actor,
            new SystemSettingSaveDto
            {
                Values = { [MercadoLivreSearchCacheRules.SettingKey] = minutes.ToString(CultureInfo.InvariantCulture) }
            },
            cancellationToken);
        if (!saved.Success)
        {
            return saved;
        }

        return new MasterOperationResultDto
        {
            Success = true,
            Message = minutes == 0
                ? "Cache de buscas desligado: toda busca consulta a API do Mercado Livre."
                : $"Tempo de cache de buscas: {minutes} minuto(s). Já vale para as próximas buscas."
        };
    }

    public async Task<MasterOperationResultDto> DisconnectMercadoLivreAsync(
        MasterActor actor,
        CancellationToken cancellationToken = default)
    {
        var token = await _context.GlobalIntegrationTokens
            .FirstOrDefaultAsync(item => item.Provider == MarketplaceType.MercadoLivre, cancellationToken);
        if (token is null)
        {
            return new MasterOperationResultDto { Success = true, Message = "A API Mercado Livre já está desconectada." };
        }

        _context.GlobalIntegrationTokens.Remove(token);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actor, MasterAuditActions.GlobalIntegrationDisconnected, TargetType, ProviderKey,
            "Token Master da API Mercado Livre removido.", cancellationToken);
        return new MasterOperationResultDto
        {
            Success = true,
            Message = "API Mercado Livre desconectada. As buscas voltam ao token do aplicativo até uma nova autorização."
        };
    }

    public async Task<MasterOperationResultDto> CompleteMercadoLivreAuthorizationAsync(
        int userId,
        string code,
        string? redirectUri,
        string? codeVerifier,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.UserAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.Id == userId && item.IsActive)
            .Select(item => new { item.Id, item.Email, item.Role })
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null || !MasterPermissionMatrix.Has(user.Role, MasterPermissions.ManageGlobalKeys))
        {
            _logger.LogWarning("[ML OAuth Master] Autorização recusada: usuário {UserId} sem permissão de chaves globais.", userId);
            return Fail("Somente operadores Master com acesso às chaves globais podem autorizar a API Mercado Livre.");
        }

        var exchange = await _marketplaceAuth.ExchangeMercadoLivreCodeAsync(code, redirectUri, codeVerifier, cancellationToken);
        if (!exchange.Success || string.IsNullOrWhiteSpace(exchange.AccessToken))
        {
            return Fail(string.IsNullOrWhiteSpace(exchange.ErrorMessage)
                ? "O Mercado Livre não devolveu o token de acesso. Tente autorizar novamente."
                : exchange.ErrorMessage);
        }

        var token = await _context.GlobalIntegrationTokens
            .FirstOrDefaultAsync(item => item.Provider == MarketplaceType.MercadoLivre, cancellationToken);
        if (token is null)
        {
            token = new GlobalIntegrationToken { Provider = MarketplaceType.MercadoLivre };
            _context.GlobalIntegrationTokens.Add(token);
        }

        var now = DateTime.UtcNow;
        token.AccessToken = exchange.AccessToken.Trim();
        token.RefreshToken = string.IsNullOrWhiteSpace(exchange.RefreshToken) ? null : exchange.RefreshToken.Trim();
        token.ExpiresAt = exchange.ExpiresAt;
        token.RefreshExpiresAt = exchange.RefreshExpiresAt;
        token.LastRefreshAt = null;
        token.LastError = null;
        token.AuthorizedAt = now;
        token.AuthorizedByUserId = user.Id;
        token.AuthorizedBy = user.Email;
        token.Touch();
        await _context.SaveChangesAsync(cancellationToken);

        var actor = new MasterActor(user.Id, user.Email, user.Role, null);
        await _audit.LogAsync(actor, MasterAuditActions.GlobalIntegrationAuthorized, TargetType, ProviderKey,
            $"API Mercado Livre autorizada (token válido até {exchange.ExpiresAt:yyyy-MM-dd HH:mm} UTC).", cancellationToken);
        _logger.LogInformation(
            "[ML OAuth Master] API Mercado Livre autorizada por {Email}; access token válido até {ExpiresAt:u}.",
            user.Email,
            exchange.ExpiresAt);
        return new MasterOperationResultDto
        {
            Success = true,
            Message = "API Mercado Livre autorizada. As buscas de todos os clientes já usam o token Master."
        };
    }

    public async Task<GlobalIntegrationStatusDto> GetTikTokShopStatusAsync(CancellationToken cancellationToken = default)
    {
        var token = await LoadTokenAsync(MarketplaceType.TikTokShop, tracking: false, cancellationToken);
        var now = DateTime.UtcNow;
        var options = TikTok;
        var status = GlobalIntegrationRules.ResolveStatus(token, now);
        return new GlobalIntegrationStatusDto
        {
            Provider = GlobalIntegrationRules.TikTokShopKey,
            ProviderLabel = "TikTok Shop",
            Status = status,
            ExpiresAt = token?.ExpiresAt,
            RenewsOnNextCall = status == GlobalIntegrationRules.StatusActive && !GlobalIntegrationRules.IsAccessTokenFresh(token, now),
            RefreshExpiresAt = token?.RefreshExpiresAt,
            LastRefreshAt = token?.LastRefreshAt,
            LastError = token?.LastError,
            AuthorizedBy = token?.AuthorizedBy,
            AuthorizedAt = token?.AuthorizedAt,
            HasAppCredentials = options.HasAppCredentials,
            SearchCacheMinutes = options.ResolveSearchCacheMinutes(),
            AccountId = token?.ExternalAccountId,
            AccountName = token?.ExternalAccountName
        };
    }

    public async Task<MasterOperationResultDto> SaveTikTokShopSettingsAsync(
        MasterActor actor,
        GlobalIntegrationSettingsDto request,
        CancellationToken cancellationToken = default)
    {
        var minutes = request.SearchCacheMinutes;
        if (minutes < 0 || minutes > TikTokShopSearchCacheRules.MaxMinutes)
        {
            return Fail($"Informe um tempo de cache entre 0 e {TikTokShopSearchCacheRules.MaxMinutes} minutos (0 desliga o cache).");
        }

        var saved = await _settings.SaveAsync(
            actor,
            new SystemSettingSaveDto
            {
                Values = { [TikTokShopSearchCacheRules.SettingKey] = minutes.ToString(CultureInfo.InvariantCulture) }
            },
            cancellationToken);
        if (!saved.Success)
        {
            return saved;
        }

        return new MasterOperationResultDto
        {
            Success = true,
            Message = minutes == 0
                ? "Cache de buscas desligado: toda busca consulta a API do TikTok Shop."
                : $"Tempo de cache de buscas do TikTok Shop: {minutes} minuto(s). Já vale para as próximas buscas."
        };
    }

    public async Task<MasterOperationResultDto> DisconnectTikTokShopAsync(
        MasterActor actor,
        CancellationToken cancellationToken = default)
    {
        var token = await LoadTokenAsync(MarketplaceType.TikTokShop, tracking: true, cancellationToken);
        if (token is null)
        {
            return new MasterOperationResultDto { Success = true, Message = "A API TikTok Shop já está desconectada." };
        }

        _context.GlobalIntegrationTokens.Remove(token);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actor, MasterAuditActions.GlobalIntegrationDisconnected, TargetType, GlobalIntegrationRules.TikTokShopKey,
            "Token Master da API TikTok Shop removido.", cancellationToken);
        return new MasterOperationResultDto
        {
            Success = true,
            Message = "API TikTok Shop desconectada. As buscas do TikTok Shop ficam indisponíveis até uma nova autorização."
        };
    }

    public async Task<MasterOperationResultDto> CompleteTikTokShopAuthorizationAsync(
        int userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.UserAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.Id == userId && item.IsActive)
            .Select(item => new { item.Id, item.Email, item.Role })
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null || !MasterPermissionMatrix.Has(user.Role, MasterPermissions.ManageGlobalKeys))
        {
            _logger.LogWarning("[TikTok OAuth Master] Autorização recusada: usuário {UserId} sem permissão de chaves globais.", userId);
            return Fail("Somente operadores Master com acesso às chaves globais podem autorizar a API TikTok Shop.");
        }

        var exchange = await _marketplaceAuth.ExchangeTikTokShopCodeAsync(code, cancellationToken);
        if (!exchange.Success || string.IsNullOrWhiteSpace(exchange.AccessToken))
        {
            return Fail(string.IsNullOrWhiteSpace(exchange.ErrorMessage)
                ? "O TikTok Shop não devolveu o token de acesso. Tente autorizar novamente."
                : exchange.ErrorMessage);
        }

        if (!TikTokShopUserTypes.IsCreator(exchange.UserType))
        {
            _logger.LogWarning("[TikTok OAuth Master] Conta autorizada não é de criador (user_type={UserType}).", exchange.UserType);
            return Fail("A conta autorizada não é de criador (afiliado). Autorize com a conta de criador TikTok da Ofertio.");
        }

        var token = await LoadTokenAsync(MarketplaceType.TikTokShop, tracking: true, cancellationToken);
        if (token is null)
        {
            token = new GlobalIntegrationToken { Provider = MarketplaceType.TikTokShop };
            _context.GlobalIntegrationTokens.Add(token);
        }

        var now = DateTime.UtcNow;
        token.AccessToken = exchange.AccessToken.Trim();
        token.RefreshToken = string.IsNullOrWhiteSpace(exchange.RefreshToken) ? null : exchange.RefreshToken.Trim();
        token.ExpiresAt = exchange.ExpiresAt;
        token.RefreshExpiresAt = exchange.RefreshExpiresAt;
        token.LastRefreshAt = null;
        token.LastError = null;
        token.AuthorizedAt = now;
        token.AuthorizedByUserId = user.Id;
        token.AuthorizedBy = user.Email;
        token.ExternalAccountId = Truncate(exchange.OpenId, 128);
        token.ExternalAccountName = Truncate(exchange.AccountName, 256);
        token.Touch();
        await _context.SaveChangesAsync(cancellationToken);

        var actor = new MasterActor(user.Id, user.Email, user.Role, null);
        await _audit.LogAsync(actor, MasterAuditActions.GlobalIntegrationAuthorized, TargetType, GlobalIntegrationRules.TikTokShopKey,
            $"API TikTok Shop autorizada (token válido até {exchange.ExpiresAt:yyyy-MM-dd HH:mm} UTC).", cancellationToken);
        _logger.LogInformation(
            "[TikTok OAuth Master] API TikTok Shop autorizada por {Email}; access token válido até {ExpiresAt:u}.",
            user.Email,
            exchange.ExpiresAt);
        return new MasterOperationResultDto
        {
            Success = true,
            Message = "API TikTok Shop autorizada. As buscas de todos os clientes já usam o token Master."
        };
    }

    private Task<GlobalIntegrationToken?> LoadTokenAsync(MarketplaceType provider, bool tracking, CancellationToken cancellationToken)
    {
        var query = tracking ? _context.GlobalIntegrationTokens : _context.GlobalIntegrationTokens.AsNoTracking();
        return query.FirstOrDefaultAsync(item => item.Provider == provider, cancellationToken);
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length > max ? value.Trim()[..max] : value.Trim();

    private static MasterOperationResultDto Fail(string message) => new() { Success = false, Message = message };
}

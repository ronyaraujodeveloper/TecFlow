using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Master;

/// <summary>Token Master da API oficial do Mercado Livre e configurações globais da integração.</summary>
public sealed class GlobalIntegrationService : IGlobalIntegrationService
{
    private const string TargetType = "GlobalIntegrationToken";
    private const string ProviderKey = "mercado-livre";

    private readonly AppDbContext _context;
    private readonly IMarketplaceAuthService _marketplaceAuth;
    private readonly ISystemSettingsService _settings;
    private readonly IMasterAuditService _audit;
    private readonly IOptionsMonitor<MercadoLivreIntegrationOptions> _options;
    private readonly ILogger<GlobalIntegrationService> _logger;

    public GlobalIntegrationService(
        AppDbContext context,
        IMarketplaceAuthService marketplaceAuth,
        ISystemSettingsService settings,
        IMasterAuditService audit,
        IOptionsMonitor<MercadoLivreIntegrationOptions> options,
        ILogger<GlobalIntegrationService> logger)
    {
        _context = context;
        _marketplaceAuth = marketplaceAuth;
        _settings = settings;
        _audit = audit;
        _options = options;
        _logger = logger;
    }

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

    private static MasterOperationResultDto Fail(string message) => new() { Success = false, Message = message };
}

using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Integrations.Common;
using Ofertio.Business.Integrations.TikTokShop;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Integrations.TikTokShop;

/// <summary>
/// Busca Affiliate Creator do TikTok Shop com o token Master (Integrações Globais). Token vencido é renovado via
/// <c>refresh_token</c>; recusado (401/código de token) é renovado e a busca é repetida uma vez. Resultados ficam no
/// cache global pelo tempo configurado no Painel Master.
/// </summary>
public sealed class TikTokShopAffiliateApiService : ITikTokShopAffiliateApiService
{
    public const string HttpClientName = "Ofertio.TikTokShopAffiliate";

    private static readonly SemaphoreSlim MasterRefreshLock = new(1, 1);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IMarketplaceAuthService _authService;
    private readonly IOptionsMonitor<TikTokShopIntegrationOptions> _options;
    private readonly ILogger<TikTokShopAffiliateApiService> _logger;
    private readonly IMemoryCache? _cache;

    public TikTokShopAffiliateApiService(
        IHttpClientFactory httpClientFactory,
        IDbContextFactory<AppDbContext> dbContextFactory,
        IMarketplaceAuthService authService,
        IOptionsMonitor<TikTokShopIntegrationOptions> options,
        ILogger<TikTokShopAffiliateApiService> logger,
        IMemoryCache? cache = null)
    {
        _httpClientFactory = httpClientFactory;
        _dbContextFactory = dbContextFactory;
        _authService = authService;
        _options = options;
        _logger = logger;
        _cache = cache;
    }

    public async Task<OfficialCatalogChannelResult> SearchProductsAsync(
        TikTokShopProductSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        if (!options.HasAppCredentials)
        {
            return OfficialCatalogChannelResult.Unconfigured();
        }

        var cacheMinutes = _cache is null ? 0 : options.ResolveSearchCacheMinutes();
        var cacheKey = cacheMinutes > 0 ? TikTokShopSearchCacheRules.BuildKey(request) : null;
        if (cacheKey is not null && _cache!.TryGetValue(cacheKey, out OfficialCatalogChannelResult? cached) && cached is not null)
        {
            return TikTokShopSearchCacheRules.Clone(cached);
        }

        var token = await LoadMasterTokenAsync(cancellationToken);
        if (token is null)
        {
            return OfficialCatalogChannelResult.Unconfigured();
        }

        var accessToken = GlobalIntegrationRules.IsAccessTokenFresh(token, DateTime.UtcNow)
            ? token.AccessToken.Trim()
            : string.IsNullOrWhiteSpace(token.LastError)
                ? await RefreshMasterTokenAsync(token.AccessToken, cancellationToken)
                : null;
        if (accessToken is null)
        {
            return OfficialCatalogChannelResult.Failed(TikTokShopAffiliateSearchRules.MasterTokenExpiredMessage);
        }

        var (result, refused) = await SendSearchAsync(options, accessToken, request, cancellationToken);
        if (refused)
        {
            _logger.LogWarning("[TikTok Shop Busca] Token Master recusado; renovando via refresh_token e repetindo a busca.");
            var renewed = await RefreshMasterTokenAsync(accessToken, cancellationToken);
            if (renewed is null)
            {
                return OfficialCatalogChannelResult.Failed(TikTokShopAffiliateSearchRules.MasterTokenExpiredMessage);
            }

            (result, refused) = await SendSearchAsync(options, renewed, request, cancellationToken);
            if (refused)
            {
                return OfficialCatalogChannelResult.Failed(TikTokShopAffiliateSearchRules.MasterTokenExpiredMessage);
            }
        }

        if (cacheKey is not null && TikTokShopSearchCacheRules.IsCacheable(result))
        {
            _cache!.Set(cacheKey, TikTokShopSearchCacheRules.Clone(result), TimeSpan.FromMinutes(cacheMinutes));
        }

        return result;
    }

    private async Task<(OfficialCatalogChannelResult Result, bool Refused)> SendSearchAsync(
        TikTokShopIntegrationOptions options,
        string accessToken,
        TikTokShopProductSearchRequest request,
        CancellationToken cancellationToken)
    {
        var path = "/" + options.AffiliateProductSearchPath.Trim().TrimStart('/');
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var body = TikTokShopAffiliateSearchRules.BuildBody(request);
        var query = TikTokShopAffiliateSearchRules.BuildQuery(request);
        var appKey = options.AppKey.Trim();
        var sign = MarketplaceSignatureHelper.GenerateTikTokShopSign(appKey, options.AppSecret.Trim(), path, timestamp, body, query);

        var url = new StringBuilder(options.ApiBaseUrl.TrimEnd('/'))
            .Append(path)
            .Append("?app_key=").Append(Uri.EscapeDataString(appKey))
            .Append("&timestamp=").Append(timestamp.ToString(CultureInfo.InvariantCulture))
            .Append("&sign=").Append(sign);
        foreach (var (key, value) in query)
        {
            url.Append('&').Append(key).Append('=').Append(Uri.EscapeDataString(value ?? string.Empty));
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, url.ToString())
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        message.Headers.TryAddWithoutValidation("x-tts-access-token", accessToken);

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            client.Timeout = options.ResolveHttpTimeout();
            using var response = await client.SendAsync(message, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = TikTokShopAffiliateSearchRules.Parse(json, out var errorCode, out var errorMessage);
            if (TikTokShopAffiliateSearchRules.IsTokenRefused(response.StatusCode, errorCode))
            {
                return (result, true);
            }

            if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                result = OfficialCatalogChannelResult.Failed($"TikTok Shop respondeu HTTP {(int)response.StatusCode}.");
            }

            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                _logger.LogWarning(
                    "[TikTok Shop Busca] API recusou a busca. Status={Status} Code={Code} Message={Message}",
                    (int)response.StatusCode,
                    errorCode,
                    errorMessage);
            }

            return (result, false);
        }
        catch (System.Text.Json.JsonException)
        {
            return (OfficialCatalogChannelResult.Failed("Erro de Desserialização"), false);
        }
        catch (Exception ex) when (ex is TaskCanceledException or TimeoutException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("[TikTok Shop Busca] A API excedeu o tempo limite.");
            return (OfficialCatalogChannelResult.Failed(TikTokShopAffiliateSearchRules.TimeoutMessage), false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "[TikTok Shop Busca] Falha de rede ao consultar a API.");
            return (OfficialCatalogChannelResult.Failed("Não foi possível conectar à API do TikTok Shop."), false);
        }
    }

    private async Task<GlobalIntegrationToken?> LoadMasterTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var token = await dbContext.GlobalIntegrationTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Provider == MarketplaceType.TikTokShop, cancellationToken);
            return token is null
                   || string.IsNullOrWhiteSpace(token.AccessToken)
                   || HomologMarketplaceAuth.IsStubToken(token.AccessToken)
                ? null
                : token;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "[TikTok Shop Master] Não foi possível ler o token Master.");
            return null;
        }
    }

    /// <summary>
    /// Renova o token Master e grava no banco. Serializado: requisições simultâneas que encontram o token já renovado
    /// por outra reutilizam o novo valor.
    /// </summary>
    private async Task<string?> RefreshMasterTokenAsync(string staleAccessToken, CancellationToken cancellationToken)
    {
        await MasterRefreshLock.WaitAsync(cancellationToken);
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var token = await dbContext.GlobalIntegrationTokens
                .FirstOrDefaultAsync(item => item.Provider == MarketplaceType.TikTokShop, cancellationToken);
            if (token is null)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            if (!string.Equals(token.AccessToken, staleAccessToken, StringComparison.Ordinal)
                && GlobalIntegrationRules.IsAccessTokenFresh(token, now))
            {
                return token.AccessToken.Trim();
            }

            if (!GlobalIntegrationRules.CanRefresh(token, now) || HomologMarketplaceAuth.IsStubToken(token.RefreshToken))
            {
                await RecordMasterErrorAsync(dbContext, token, "Token Master expirado sem refresh_token válido: reautorize no Painel Master.", cancellationToken);
                return null;
            }

            var refreshed = await _authService.RefreshTikTokShopTokenAsync(token.RefreshToken!, cancellationToken);
            if (!refreshed.Success || string.IsNullOrWhiteSpace(refreshed.AccessToken))
            {
                await RecordMasterErrorAsync(dbContext, token, "O TikTok Shop recusou o refresh_token Master: reautorize no Painel Master.", cancellationToken);
                return null;
            }

            token.AccessToken = refreshed.AccessToken.Trim();
            if (!string.IsNullOrWhiteSpace(refreshed.RefreshToken))
            {
                token.RefreshToken = refreshed.RefreshToken.Trim();
            }

            token.ExpiresAt = refreshed.ExpiresAt;
            token.RefreshExpiresAt = refreshed.RefreshExpiresAt ?? token.RefreshExpiresAt;
            token.LastRefreshAt = now;
            token.LastError = null;
            token.Touch();
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "[TikTok Shop Master] Token Master renovado automaticamente; válido até {ExpiresAt:u}.",
                token.ExpiresAt);
            return token.AccessToken;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "[TikTok Shop Master] Falha ao renovar o token Master.");
            return null;
        }
        finally
        {
            MasterRefreshLock.Release();
        }
    }

    private async Task RecordMasterErrorAsync(
        AppDbContext dbContext,
        GlobalIntegrationToken token,
        string error,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("[TikTok Shop Master] {Error}", error);
        token.LastError = GlobalIntegrationRules.TruncateError(error);
        token.Touch();
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Radar;

public sealed class MercadoLivreApiService : IMercadoLivreApiService
{
    public const string PublicUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36";

    private static readonly JsonSerializerOptions SearchJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly ConcurrentDictionary<string, (string Token, DateTime ExpiresAt)> AppTokenCache =
        new(StringComparer.Ordinal);

    private static readonly TimeSpan NotConnectedLogInterval = TimeSpan.FromMinutes(30);

    private static readonly ConcurrentDictionary<int, DateTime> NotConnectedLoggedAt = new();

    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<MercadoLivreIntegrationOptions> _options;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<MercadoLivreApiService> _logger;
    private readonly MercadoLivreAntiBotGuard? _antiBotGuard;

    public MercadoLivreApiService(
        HttpClient httpClient,
        IOptionsMonitor<MercadoLivreIntegrationOptions> options,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ILogger<MercadoLivreApiService> logger,
        MercadoLivreAntiBotGuard? antiBotGuard = null)
    {
        _httpClient = httpClient;
        _options = options;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _antiBotGuard = antiBotGuard;
        EnsurePublicUserAgent(_httpClient);
    }

    /// <summary>
    /// <c>GET https://api.mercadolibre.com/items/{id}</c> com <c>Authorization: Bearer</c>: token OAuth da conta
    /// conectada no painel (renovado via refresh_token quando expira ou é recusado) ➔ token fixo ➔ token do app.
    /// Sem usuário (<paramref name="userId"/> ≤ 0, rotinas em segundo plano) não usa conta de nenhum inquilino.
    /// </summary>
    public async Task<OfficialOfferSnapshotDto?> GetItemAsync(
        int userId,
        string itemId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        itemId = itemId.Trim();
        try
        {
            var account = userId > 0 ? await LoadMercadoLivreAccountAsync(userId, cancellationToken) : null;
            var accountToken = await ResolveAccountAccessTokenAsync(account, cancellationToken);
            if (userId > 0)
            {
                LogItemAccountState(userId, itemId, account, accountToken);
            }

            var accessToken = accountToken
                ?? ResolveStaticAccessToken()
                ?? await GetAppAccessTokenAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogWarning(
                    "[ML Item] Sem access_token para consultar o item {ItemId}: conecte a conta Mercado Livre em Minhas Lojas ou configure Integrations:MercadoLivre:AppId/SecretKey.",
                    itemId);
                return null;
            }

            var (statusCode, body) = await SendItemAsync(itemId, accessToken, cancellationToken);
            if ((statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) && accountToken is not null)
            {
                _logger.LogWarning(
                    "[ML Item] API recusou o token da conta '{AccountName}' (Id={AccountId}) para o item {ItemId} [{StatusCode}]; renovando via refresh_token.",
                    DescribeAccount(account!),
                    account!.Id,
                    itemId,
                    (int)statusCode);
                var renewedToken = await RefreshAccountTokenAsync(account, cancellationToken);
                if (renewedToken is not null)
                {
                    accessToken = renewedToken;
                    (statusCode, body) = await SendItemAsync(itemId, accessToken, cancellationToken);
                }
            }

            if (statusCode == HttpStatusCode.NotFound)
            {
                return await GetCatalogBuyBoxItemAsync(itemId, accessToken, body, cancellationToken);
            }

            if (statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("[ML Item] Erro API ML [{StatusCode}] no item {ItemId}: {Body}", statusCode, itemId, body);
                return null;
            }

            return MercadoLivreItemParser.Parse(body);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "[ML Item] Exceção ao consultar item Mercado Livre '{Item}'", itemId);
            return null;
        }
    }

    private void LogItemAccountState(int userId, string itemId, MarketplaceAccount? account, string? accountToken)
    {
        if (account is null || (!HasOAuthToken(account) && string.IsNullOrWhiteSpace(account.RefreshToken)))
        {
            var now = DateTime.UtcNow;
            if (NotConnectedLoggedAt.TryGetValue(userId, out var loggedAt) && now - loggedAt < NotConnectedLogInterval)
            {
                return;
            }

            NotConnectedLoggedAt[userId] = now;
            _logger.LogWarning(
                "[ML Item] Nenhuma conta Mercado Livre conectada/autorizada no painel para o usuário {UserId}. Conecte em Minhas Lojas ➔ Mercado Livre (OAuth) para consultar o item {ItemId} com o token da loja.",
                userId,
                itemId);
            return;
        }

        if (accountToken is null)
        {
            _logger.LogWarning(
                "[ML Item] Token OAuth da conta '{AccountName}' (Id={AccountId}) expirado e o refresh_token não renovou o acesso. Reconecte a conta Mercado Livre em Minhas Lojas.",
                DescribeAccount(account),
                account.Id);
        }
    }

    /// <summary>
    /// Links <c>/p/MLB...</c> trazem o código do produto de catálogo, que <c>/items</c> não conhece (404):
    /// consulta <c>/products/{id}</c> e segue para o anúncio ganhador da buy box.
    /// </summary>
    private async Task<OfficialOfferSnapshotDto?> GetCatalogBuyBoxItemAsync(
        string catalogId,
        string accessToken,
        string itemNotFoundBody,
        CancellationToken cancellationToken)
    {
        var (productStatus, productBody) = await SendApiAsync("products", catalogId, accessToken, cancellationToken);
        if (productStatus != HttpStatusCode.OK)
        {
            _logger.LogError(
                "[ML Item] Erro API ML [{StatusCode}] no item {ItemId} (nem anúncio nem produto de catálogo): {Body}",
                HttpStatusCode.NotFound,
                catalogId,
                itemNotFoundBody);
            return null;
        }

        var (winnerItemId, productName, productImage) = MercadoLivreItemParser.ParseCatalogProduct(productBody);
        if (winnerItemId is null)
        {
            _logger.LogInformation(
                "[ML Item] Produto de catálogo {CatalogId} sem anúncio ganhador (buy_box_winner) na API oficial.",
                catalogId);
            return null;
        }

        var (itemStatus, itemBody) = await SendItemAsync(winnerItemId, accessToken, cancellationToken);
        if (itemStatus != HttpStatusCode.OK)
        {
            _logger.LogError(
                "[ML Item] Erro API ML [{StatusCode}] no anúncio {ItemId} do catálogo {CatalogId}: {Body}",
                itemStatus,
                winnerItemId,
                catalogId,
                itemBody);
            return null;
        }

        var snapshot = MercadoLivreItemParser.Parse(itemBody);
        if (snapshot is not null)
        {
            snapshot.ProductName ??= productName;
            snapshot.ImageUrl ??= productImage;
        }

        return snapshot;
    }

    private Task<(HttpStatusCode StatusCode, string Body)> SendItemAsync(
        string itemId,
        string accessToken,
        CancellationToken cancellationToken) =>
        SendApiAsync("items", itemId, accessToken, cancellationToken);

    private async Task<(HttpStatusCode StatusCode, string Body)> SendApiAsync(
        string resource,
        string id,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.mercadolibre.com/{resource}/{Uri.EscapeDataString(id)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());
        ApplyBrowserHeaders(request);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return (response.StatusCode, body);
    }

    public async Task<OfficialCatalogChannelResult> SearchProductsAsync(
        int userId,
        string query,
        int limit = 20,
        string? trackingId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return OfficialCatalogChannelResult.Empty;
        }

        query = query.Trim();
        var maxItems = OfficialCatalogSearchRules.ClampLimit(limit);
        var account = await LoadMercadoLivreAccountAsync(userId, cancellationToken);
        var affiliateId = OfficialCatalogSearchRules.ResolveMercadoLivreAffiliateId(
            FirstNonEmpty(trackingId, account?.TrackingId, account?.AffiliateTrackingId),
            null);
        if (!_options.CurrentValue.UseOfficialSearchApi)
        {
            var publicItems = await TrySearchPublicApiAsync(query, affiliateId, maxItems, cancellationToken);
            if (publicItems is not null)
            {
                return ToChannelResult(publicItems);
            }

            _logger.LogInformation(
                "[ML Busca] API pública recusou '{Query}'; buscando na vitrine lista.mercadolivre.com.br.",
                query);
            var primary = await SearchViaListaAsync(query, affiliateId, maxItems, cancellationToken);
            if (primary.Items.Count > 0)
            {
                return OfficialCatalogChannelResult.From(primary.Items);
            }

            return primary.Blocked
                ? OfficialCatalogChannelResult.Failed(OfficialCatalogSearchRules.MercadoLivreScraperBlockedMessage)
                : OfficialCatalogChannelResult.Empty;
        }

        var accountToken = await ResolveAccountAccessTokenAsync(account, cancellationToken);
        var accessToken = accountToken
            ?? ResolveStaticAccessToken()
            ?? await GetAppAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            var scraped = await SearchViaListaAsync(query, affiliateId, maxItems, cancellationToken);
            return scraped.Items.Count > 0
                ? OfficialCatalogChannelResult.From(scraped.Items)
                : OfficialCatalogChannelResult.Unconfigured();
        }

        if (accountToken is not null)
        {
            LogAccountTokenUsage(account!, query, renewed: false);
        }

        try
        {
            var apiItems = await SearchViaApiAsync(query, accessToken.Trim(), affiliateId, maxItems, cancellationToken);
            return ToChannelResult(apiItems);
        }
        catch (MercadoLivreSearchForbiddenException ex) when (accountToken is not null)
        {
            _logger.LogWarning(
                "[ML Busca] API MLB recusou o token da conta '{AccountName}' (Id={AccountId}) para '{Query}' ({Reason}); renovando via refresh_token.",
                DescribeAccount(account!),
                account!.Id,
                query,
                ex.Message);
            return await RetryWithRenewedAccountTokenAsync(account, query, affiliateId, maxItems, cancellationToken);
        }
        catch (MercadoLivreSearchForbiddenException ex)
        {
            _logger.LogWarning("API MLB recusou a busca de '{Query}' ({Reason}); usando a vitrine lista.mercadolivre.com.br.", query, ex.Message);
            EvictAppToken(accessToken);
            return await SearchListaFallbackAsync(query, affiliateId, maxItems, cancellationToken);
        }
        catch (Exception ex)
        {
            var tracedError = string.IsNullOrWhiteSpace(ex.Message)
                ? OfficialCatalogSearchRules.MercadoLivreSearchForbiddenMessage
                : ex.Message;
            _logger.LogError(ex, "Erro na busca API do Mercado Livre para '{Query}': {Error}", query, tracedError);
            return OfficialCatalogChannelResult.Failed(tracedError);
        }
    }

    private async Task<IReadOnlyList<OfficialCatalogProductDto>?> TrySearchPublicApiAsync(
        string query,
        string? affiliateId,
        int maxItems,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await SearchViaApiAsync(query, accessToken: null, affiliateId, maxItems, cancellationToken);
            _logger.LogInformation(
                "[ML Busca] API pública sites/MLB/search devolveu {Count} ofertas para '{Query}' (matt_tool={AffiliateId}).",
                items.Count,
                query,
                affiliateId ?? "-");
            return items;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("[ML Busca] API pública sites/MLB/search indisponível para '{Query}' ({Reason}).", query, ex.Message);
            return null;
        }
    }

    private async Task<OfficialCatalogChannelResult> RetryWithRenewedAccountTokenAsync(
        MarketplaceAccount account,
        string query,
        string? affiliateId,
        int maxItems,
        CancellationToken cancellationToken)
    {
        var renewedToken = await RefreshAccountTokenAsync(account, cancellationToken);
        if (renewedToken is null)
        {
            _logger.LogWarning(
                "[ML Busca] Não foi possível renovar o token da conta '{AccountName}' (Id={AccountId}); usando a vitrine lista.mercadolivre.com.br.",
                DescribeAccount(account),
                account.Id);
            return await SearchListaFallbackAsync(query, affiliateId, maxItems, cancellationToken);
        }

        LogAccountTokenUsage(account, query, renewed: true);
        try
        {
            var apiItems = await SearchViaApiAsync(query, renewedToken, affiliateId, maxItems, cancellationToken);
            return ToChannelResult(apiItems);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "[ML Busca] API MLB recusou também o token renovado da conta '{AccountName}' (Id={AccountId}) ({Reason}); usando a vitrine lista.mercadolivre.com.br.",
                DescribeAccount(account),
                account.Id,
                ex.Message);
            return await SearchListaFallbackAsync(query, affiliateId, maxItems, cancellationToken);
        }
    }

    private async Task<OfficialCatalogChannelResult> SearchListaFallbackAsync(
        string query,
        string? affiliateId,
        int maxItems,
        CancellationToken cancellationToken)
    {
        var scraped = await SearchViaListaAsync(query, affiliateId, maxItems, cancellationToken);
        return scraped.Items.Count > 0
            ? OfficialCatalogChannelResult.From(scraped.Items)
            : OfficialCatalogChannelResult.Failed(OfficialCatalogSearchRules.MercadoLivreSearchForbiddenMessage);
    }

    private static OfficialCatalogChannelResult ToChannelResult(IReadOnlyList<OfficialCatalogProductDto> items) =>
        items.Count > 0 ? OfficialCatalogChannelResult.From(items) : OfficialCatalogChannelResult.Empty;

    private void LogAccountTokenUsage(MarketplaceAccount account, string query, bool renewed) =>
        _logger.LogInformation(
            "[ML Busca] Busca '{Query}' realizada com o token OAuth {TokenKind} da conta '{AccountName}' (Id={AccountId}, Tenant={TenantId}).",
            query,
            renewed ? "renovado" : "armazenado",
            DescribeAccount(account),
            account.Id,
            account.TenantId);

    private static string DescribeAccount(MarketplaceAccount account) =>
        FirstNonEmpty(account.FriendlyName, account.ShopName) ?? $"#{account.Id}";

    internal static void EnsurePublicUserAgent(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        httpClient.DefaultRequestHeaders.Remove("User-Agent");
        ApplyHeader(httpClient.DefaultRequestHeaders, "User-Agent", PublicUserAgent);
        if (!httpClient.DefaultRequestHeaders.Accept.Any(item => item.MediaType == "application/json"))
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    private async Task<IReadOnlyList<OfficialCatalogProductDto>> SearchViaApiAsync(
        string query,
        string? accessToken,
        string? affiliateId,
        int maxItems,
        CancellationToken cancellationToken)
    {
        var searchUri = $"https://api.mercadolibre.com/sites/MLB/search?q={Uri.EscapeDataString(query)}&limit={maxItems}";
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            searchUri += $"&access_token={Uri.EscapeDataString(accessToken)}";
        }

        var (statusCode, body) = await SendSearchAsync(searchUri, accessToken, cancellationToken);
        if (OfficialCatalogSearchRules.IsMercadoLivreSearchRefused(statusCode, body))
        {
            _logger.LogError("Erro API ML [{StatusCode}]: {Body}", statusCode, body);
            throw new MercadoLivreSearchForbiddenException($"HTTP {(int)statusCode}");
        }

        if (statusCode != HttpStatusCode.OK)
        {
            _logger.LogError("Erro API ML [{StatusCode}]: {Body}", statusCode, body);
            throw new HttpRequestException($"ML HTTP {(int)statusCode} ({statusCode})");
        }

        var payload = JsonSerializer.Deserialize<MercadoLivreSearchResponse>(body, SearchJsonOptions);
        if (payload?.Results == null || payload.Results.Count == 0)
        {
            return [];
        }

        return payload.Results
            .Select(item => new OfficialCatalogProductDto
            {
                Platform = "Mercado Livre",
                PlatformType = MarketplaceType.MercadoLivre,
                ProductId = string.IsNullOrWhiteSpace(item.Id) ? null : item.Id,
                ProductName = string.IsNullOrWhiteSpace(item.Title) ? null : item.Title,
                Price = item.Price,
                OriginalPrice = item.OriginalPrice,
                ImageUrl = string.IsNullOrWhiteSpace(item.SecureThumbnail)
                    ? item.Thumbnail?.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase)
                    : item.SecureThumbnail.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase),
                SourceUrl = MercadoLivreCommissionUrlBuilder.InjectMattTool(item.Permalink, affiliateId),
                Shipping = item.Shipping?.FreeShipping == true ? "Frete grátis" : null,
                Source = "Api"
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.SourceUrl))
            .ToList();
    }

    private async Task<(HttpStatusCode StatusCode, string Body)> SendSearchAsync(
        string searchUri,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, searchUri);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        ApplyBrowserHeaders(request);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return (response.StatusCode, body);
    }

    private async Task<(IReadOnlyList<OfficialCatalogProductDto> Items, bool Blocked)> SearchViaListaAsync(
        string query,
        string? affiliateId,
        int maxItems,
        CancellationToken cancellationToken)
    {
        var listaUrl = OfficialCatalogSearchRules.BuildMercadoLivreListaUrl(query);
        if (_antiBotGuard?.IsBlocked == true)
        {
            _logger.LogWarning(
                "[ML Busca] Vitrine pública em pausa até {BlockedUntil:HH:mm} UTC após desafio anti-robô; '{Query}' não será raspada.",
                _antiBotGuard.BlockedUntil,
                query);
            return ([], true);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, listaUrl);
            ApplyDocumentHeaders(request);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var finalUrl = response.RequestMessage?.RequestUri?.AbsoluteUri;
            if (!response.IsSuccessStatusCode || OfficialCatalogSearchRules.IsMercadoLivreVerificationPage(finalUrl, html))
            {
                if (OfficialCatalogSearchRules.IsMercadoLivreVerificationPage(finalUrl, html))
                {
                    _antiBotGuard?.Trip();
                }

                _logger.LogWarning(
                    "Vitrine ML indisponível para '{Query}' [{StatusCode}] {Url}",
                    query,
                    response.StatusCode,
                    finalUrl ?? listaUrl);
                return ([], true);
            }

            var items = MercadoLivreHtmlParser.ParseSearchResults(html)
                .Take(maxItems)
                .Select(item =>
                {
                    item.SourceUrl = MercadoLivreCommissionUrlBuilder.InjectMattTool(item.SourceUrl, affiliateId);
                    return item;
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.SourceUrl))
                .ToList();
            _logger.LogInformation(
                "[ML Busca] Vitrine lista.mercadolivre.com.br devolveu {Count} ofertas para '{Query}' (matt_tool={AffiliateId}).",
                items.Count,
                query,
                affiliateId ?? "-");
            return (items, false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Falha ao raspar a vitrine do Mercado Livre para '{Query}'.", query);
            return ([], true);
        }
    }

    private static void ApplyDocumentHeaders(HttpRequestMessage request)
    {
        ApplyHeader(request.Headers, "User-Agent", PublicUserAgent);
        ApplyHeader(request.Headers, "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        ApplyHeader(request.Headers, "Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
        ApplyHeader(request.Headers, "Sec-Ch-Ua", "\"Google Chrome\";v=\"129\", \"Not=A?Brand\";v=\"8\", \"Chromium\";v=\"129\"");
        ApplyHeader(request.Headers, "Sec-Ch-Ua-Mobile", "?0");
        ApplyHeader(request.Headers, "Sec-Ch-Ua-Platform", "\"Windows\"");
        ApplyHeader(request.Headers, "Sec-Fetch-Dest", "document");
        ApplyHeader(request.Headers, "Sec-Fetch-Mode", "navigate");
        ApplyHeader(request.Headers, "Sec-Fetch-Site", "none");
        ApplyHeader(request.Headers, "Sec-Fetch-User", "?1");
        ApplyHeader(request.Headers, "Upgrade-Insecure-Requests", "1");
    }

    private async Task<string?> GetAppAccessTokenAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (!options.HasAppCredentials)
        {
            return null;
        }

        var appId = options.AppId.Trim();
        var secret = options.SecretKey.Trim();
        var cacheKey = BuildAppTokenCacheKey(appId, secret);
        if (AppTokenCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            return cached.Token;
        }

        var appToken = await RequestOAuthTokenAsync("client_credentials", appId, secret, refreshToken: null, cancellationToken);
        if (string.IsNullOrWhiteSpace(appToken?.AccessToken))
        {
            return null;
        }

        var lifetime = appToken.ExpiresIn is > 0 ? appToken.ExpiresIn.Value : 21600;
        var token = appToken.AccessToken.Trim();
        AppTokenCache[cacheKey] = (token, DateTime.UtcNow.AddSeconds(Math.Max(60, lifetime - 300)));
        return token;
    }

    private static void EvictAppToken(string token)
    {
        foreach (var entry in AppTokenCache.Where(entry => string.Equals(entry.Value.Token, token.Trim(), StringComparison.Ordinal)))
        {
            AppTokenCache.TryRemove(entry.Key, out _);
        }
    }

    private static string BuildAppTokenCacheKey(string appId, string secret) =>
        appId + ":" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    private async Task<string?> ResolveAccountAccessTokenAsync(
        MarketplaceAccount? account,
        CancellationToken cancellationToken)
    {
        if (account is null)
        {
            return null;
        }

        if (IsTokenFresh(account))
        {
            return account.AccessToken!.Trim();
        }

        return await RefreshAccountTokenAsync(account, cancellationToken);
    }

    private async Task<string?> RefreshAccountTokenAsync(
        MarketplaceAccount account,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(account.RefreshToken)
            || HomologMarketplaceAuth.IsStubToken(account.RefreshToken)
            || !TryResolveAppCredentials(account, out var appId, out var secret))
        {
            return null;
        }

        var refreshed = await RequestOAuthTokenAsync(
            "refresh_token",
            appId,
            secret,
            account.RefreshToken,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(refreshed?.AccessToken))
        {
            return null;
        }

        await PersistRefreshedTokenAsync(account.Id, refreshed, cancellationToken);
        account.AccessToken = refreshed.AccessToken.Trim();
        if (!string.IsNullOrWhiteSpace(refreshed.RefreshToken))
        {
            account.RefreshToken = refreshed.RefreshToken.Trim();
        }

        return account.AccessToken;
    }

    private string? ResolveStaticAccessToken()
    {
        var options = _options.CurrentValue;
        return options.HasAccessToken ? options.AccessToken.Trim() : null;
    }

    private async Task PersistRefreshedTokenAsync(
        int accountId,
        MercadoLivreTokenResponse refreshed,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var tracked = await dbContext.MarketplaceAccounts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(item => item.Id == accountId, cancellationToken);
            if (tracked is null)
            {
                return;
            }

            tracked.AccessToken = refreshed.AccessToken!.Trim();
            if (!string.IsNullOrWhiteSpace(refreshed.RefreshToken))
            {
                tracked.RefreshToken = refreshed.RefreshToken.Trim();
            }

            tracked.ExpiresAt = DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn is > 0 ? refreshed.ExpiresIn.Value : 21600);
            tracked.Touch();
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token ML renovado, mas não foi possível persistir na conta {AccountId}.", accountId);
        }
    }

    private async Task<MarketplaceAccount?> LoadMercadoLivreAccountAsync(int userId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var userKey = userId.ToString(CultureInfo.InvariantCulture);
        var tenantId = await dbContext.UserAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => (Guid?)user.TenantId)
            .FirstOrDefaultAsync(cancellationToken);
        var accounts = await dbContext.MarketplaceAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.IsActive
                && (tenantId == null || x.TenantId == tenantId)
                && (x.MarketplaceType == MarketplaceType.MercadoLivre
                    || (x.FriendlyName != null && x.FriendlyName.Replace(" ", "") == "MercadoLivre")
                    || (x.ShopName != null && x.ShopName.Replace(" ", "") == "MercadoLivre")))
            .ToListAsync(cancellationToken);

        return accounts
            .Where(item => OfficialCatalogSearchRules.MatchesMercadoLivreAccount(
                item.MarketplaceType,
                item.FriendlyName,
                item.ShopName))
            .OrderByDescending(item => OwnsAccount(item, userKey))
            .ThenByDescending(HasOAuthToken)
            .ThenByDescending(item =>
                !string.IsNullOrWhiteSpace(
                    OfficialCatalogSearchRules.ResolveMercadoLivreAffiliateId(
                        item.TrackingId,
                        item.AffiliateTrackingId)))
            .ThenByDescending(item => item.ExpiresAt)
            .FirstOrDefault();
    }

    private static bool HasOAuthToken(MarketplaceAccount account) =>
        !string.IsNullOrWhiteSpace(account.AccessToken) && !HomologMarketplaceAuth.IsStubToken(account.AccessToken);

    private static bool OwnsAccount(MarketplaceAccount account, string userKey) =>
        string.IsNullOrWhiteSpace(account.UserId)
        || string.Equals(account.UserId.Trim(), userKey, StringComparison.Ordinal);

    private async Task<MercadoLivreTokenResponse?> RequestOAuthTokenAsync(
        string grantType,
        string appId,
        string secret,
        string? refreshToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var fields = new Dictionary<string, string>
            {
                ["grant_type"] = grantType,
                ["client_id"] = appId,
                ["client_secret"] = secret
            };
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                fields["refresh_token"] = refreshToken;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.CurrentValue.TokenUrl)
            {
                Content = new FormUrlEncodedContent(fields)
            };
            ApplyBrowserHeaders(request);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Falha ao renovar token ML. Status={Status} Body={Body}", response.StatusCode, json);
                return null;
            }

            var payload = JsonSerializer.Deserialize<MercadoLivreTokenResponse>(json, SearchJsonOptions);
            return string.IsNullOrWhiteSpace(payload?.AccessToken) ? null : payload;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exceção ao renovar token do Mercado Livre.");
            return null;
        }
    }

    private bool TryResolveAppCredentials(MarketplaceAccount account, out string appId, out string secret)
    {
        var options = _options.CurrentValue;
        if (options.HasAppCredentials)
        {
            appId = options.AppId.Trim();
            secret = options.SecretKey.Trim();
            return true;
        }

        appId = FirstNonEmpty(account.AppKey) ?? string.Empty;
        secret = FirstNonEmpty(account.AppSecret) ?? string.Empty;
        return !string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(secret);
    }

    private static bool IsTokenFresh(MarketplaceAccount account)
    {
        if (string.IsNullOrWhiteSpace(account.AccessToken) || HomologMarketplaceAuth.IsStubToken(account.AccessToken))
        {
            return false;
        }

        if (account.ExpiresAt.Year < 2000)
        {
            return true;
        }

        return account.ExpiresAt > DateTime.UtcNow.AddMinutes(2);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static void ApplyBrowserHeaders(HttpRequestMessage request)
    {
        ApplyHeader(request.Headers, "User-Agent", PublicUserAgent);
        ApplyHeader(request.Headers, "Accept", "application/json");
        ApplyHeader(request.Headers, "Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8");
    }

    private static void ApplyHeader(HttpHeaders headers, string name, string value)
    {
        headers.Remove(name);
        if (!headers.TryAddWithoutValidation(name, value))
        {
            headers.Add(name, value);
        }
    }

    private sealed class MercadoLivreSearchForbiddenException(string reason) : Exception(reason);

    private sealed class MercadoLivreTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }
}

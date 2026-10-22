using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
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

    private static readonly SemaphoreSlim MasterRefreshLock = new(1, 1);

    private const string ReauthorizeMasterHint = "autorize a API Mercado Livre no Painel Master ➔ Integrações Globais";

    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<MercadoLivreIntegrationOptions> _options;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<MercadoLivreApiService> _logger;
    private readonly IMemoryCache? _searchCache;

    public MercadoLivreApiService(
        HttpClient httpClient,
        IOptionsMonitor<MercadoLivreIntegrationOptions> options,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ILogger<MercadoLivreApiService> logger,
        IMemoryCache? searchCache = null)
    {
        _httpClient = httpClient;
        _options = options;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _searchCache = searchCache;
        EnsurePublicUserAgent(_httpClient);
    }

    /// <summary>
    /// <c>GET https://api.mercadolibre.com/items/{id}</c> com <c>Authorization: Bearer</c>: token Master (Integrações
    /// Globais, renovado via refresh_token quando expira ou é recusado) ➔ token OAuth legado da conta do cliente ➔
    /// token fixo ➔ token do app. Sem usuário (<paramref name="userId"/> ≤ 0) não usa conta de nenhum inquilino.
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
            var masterToken = await ResolveMasterAccessTokenAsync(cancellationToken);
            MarketplaceAccount? account = null;
            string? accountToken = null;
            if (masterToken is null)
            {
                account = userId > 0 ? await LoadMercadoLivreAccountAsync(userId, cancellationToken) : null;
                accountToken = await ResolveAccountAccessTokenAsync(account, cancellationToken);
                if (userId > 0)
                {
                    LogItemAccountState(userId, itemId, account, accountToken);
                }
            }

            var accessToken = masterToken
                ?? accountToken
                ?? ResolveStaticAccessToken()
                ?? await GetAppAccessTokenAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogWarning(
                    "[ML Item] Sem access_token para consultar o item {ItemId}: {Hint} ou configure Integrations:MercadoLivre:AppId/SecretKey.",
                    itemId,
                    ReauthorizeMasterHint);
                return null;
            }

            var (statusCode, body) = await SendItemAsync(itemId, accessToken, cancellationToken);
            if ((statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) && masterToken is not null)
            {
                _logger.LogWarning(
                    "[ML Item] API recusou o token Master para o item {ItemId} [{StatusCode}]; renovando via refresh_token.",
                    itemId,
                    (int)statusCode);
                var renewedMaster = await RefreshMasterTokenAsync(masterToken, cancellationToken);
                if (renewedMaster is not null)
                {
                    accessToken = renewedMaster;
                    (statusCode, body) = await SendItemAsync(itemId, accessToken, cancellationToken);
                }
            }
            else if ((statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) && accountToken is not null)
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
        catch (Exception ex) when (OfficialCatalogSearchRules.IsHttpTimeout(ex, cancellationToken))
        {
            _logger.LogWarning(
                "[ML Item] A API do Mercado Livre excedeu o tempo limite ({Timeout}s) ao consultar o item '{Item}'.",
                _httpClient.Timeout.TotalSeconds,
                itemId);
            return null;
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
                "[ML Item] Sem token Master da API Mercado Livre para consultar o item {ItemId} (usuário {UserId}): {Hint}.",
                itemId,
                userId,
                ReauthorizeMasterHint);
            return;
        }

        if (accountToken is null)
        {
            _logger.LogWarning(
                "[ML Item] Token OAuth legado da conta '{AccountName}' (Id={AccountId}) expirado e o refresh_token não renovou o acesso: {Hint}.",
                DescribeAccount(account),
                account.Id,
                ReauthorizeMasterHint);
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

    private Task<(HttpStatusCode StatusCode, string Body)> SendApiAsync(
        string resource,
        string id,
        string accessToken,
        CancellationToken cancellationToken) =>
        SendApiPathAsync($"{resource}/{Uri.EscapeDataString(id)}", accessToken, cancellationToken);

    private async Task<(HttpStatusCode StatusCode, string Body)> SendApiPathAsync(
        string relativePath,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.mercadolibre.com/{relativePath}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());
        ApplyBrowserHeaders(request);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return (response.StatusCode, body);
    }

    public Task<OfficialCatalogChannelResult> SearchProductsAsync(
        int userId,
        string query,
        int limit = 20,
        string? trackingId = null,
        CancellationToken cancellationToken = default) =>
        SearchCatalogAsync(
            userId,
            new MercadoLivreCatalogSearchRequest
            {
                Query = query ?? string.Empty,
                TargetOffers = limit,
                TrackingId = trackingId,
                AllowShorterQuery = false,
                IncludeCategories = false,
                IncludeMetrics = false
            },
            cancellationToken);

    public async Task<OfficialCatalogChannelResult> SearchCatalogAsync(
        int userId,
        MercadoLivreCatalogSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = request.Query?.Trim() ?? string.Empty;
        if (query.Length == 0)
        {
            return OfficialCatalogChannelResult.Empty;
        }

        var account = await LoadMercadoLivreAccountAsync(userId, cancellationToken);
        var affiliateId = OfficialCatalogSearchRules.ResolveMercadoLivreAffiliateId(
            FirstNonEmpty(request.TrackingId, account?.TrackingId, account?.AffiliateTrackingId),
            null);

        var cacheMinutes = _searchCache is null ? 0 : _options.CurrentValue.ResolveSearchCacheMinutes();
        var cacheKey = cacheMinutes > 0 ? MercadoLivreSearchCacheRules.BuildKey(query, request) : null;
        if (cacheKey is not null
            && _searchCache!.TryGetValue(cacheKey, out OfficialCatalogChannelResult? cached)
            && cached is not null)
        {
            _logger.LogInformation(
                "[ML Busca] '{Query}' servida do cache global ({Minutes} min): {OfferCount} ofertas, sem consumir a cota da API.",
                query,
                cacheMinutes,
                cached.Items.Count);
            return MercadoLivreSearchCacheRules.ForAffiliate(cached, affiliateId);
        }

        var result = await SearchCatalogFromApiAsync(account, request, query, cancellationToken);
        if (cacheKey is not null && MercadoLivreSearchCacheRules.IsCacheable(result))
        {
            _searchCache!.Set(cacheKey, MercadoLivreSearchCacheRules.Clone(result), TimeSpan.FromMinutes(cacheMinutes));
        }

        return MercadoLivreSearchCacheRules.ApplyAffiliate(result, affiliateId);
    }

    /// <summary>
    /// Busca na API sem afiliado (o <c>matt_tool</c> é aplicado por cliente depois, inclusive sobre o cache):
    /// token Master ➔ (recusado: refresh_token + nova tentativa) ➔ token OAuth legado da conta ➔ token fixo ➔ token do app.
    /// </summary>
    private async Task<OfficialCatalogChannelResult> SearchCatalogFromApiAsync(
        MarketplaceAccount? account,
        MercadoLivreCatalogSearchRequest request,
        string query,
        CancellationToken cancellationToken)
    {
        var masterToken = await ResolveMasterAccessTokenAsync(cancellationToken);
        if (masterToken is not null)
        {
            var masterResult = await SearchWithMasterTokenAsync(masterToken, request, query, cancellationToken);
            if (masterResult is not null)
            {
                return masterResult;
            }
        }

        var accountToken = await ResolveAccountAccessTokenAsync(account, cancellationToken);
        var accessToken = accountToken
            ?? ResolveStaticAccessToken()
            ?? await GetAppAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogWarning(
                "[ML Busca] Sem access_token para buscar '{Query}': {Hint} ou configure Integrations:MercadoLivre:AppId/SecretKey.",
                query,
                ReauthorizeMasterHint);
            return OfficialCatalogChannelResult.Unconfigured();
        }

        if (accountToken is not null)
        {
            LogAccountTokenUsage(account!, query, renewed: false);
        }

        try
        {
            return await ScanCatalogAsync(request, query, accessToken.Trim(), affiliateId: null, cancellationToken);
        }
        catch (MercadoLivreSearchForbiddenException ex) when (accountToken is not null)
        {
            _logger.LogWarning(
                "[ML Busca] API MLB recusou o token da conta '{AccountName}' (Id={AccountId}) para '{Query}' ({Reason}); renovando via refresh_token.",
                DescribeAccount(account!),
                account!.Id,
                query,
                ex.Message);
            return await RetryWithRenewedAccountTokenAsync(account, request, query, cancellationToken);
        }
        catch (MercadoLivreSearchForbiddenException ex)
        {
            _logger.LogWarning(
                "[ML Busca] API oficial recusou a busca de '{Query}' com o token do aplicativo ({Reason}); {Hint}.",
                query,
                ex.Message,
                ReauthorizeMasterHint);
            EvictAppToken(accessToken);
            return OfficialCatalogChannelResult.Failed(OfficialCatalogSearchRules.MercadoLivreSearchForbiddenMessage);
        }
        catch (Exception ex)
        {
            return TraceSearchError(ex, query, cancellationToken);
        }
    }

    /// <summary>Null quando o token Master foi recusado e não renovou: a busca segue para os tokens legados.</summary>
    private async Task<OfficialCatalogChannelResult?> SearchWithMasterTokenAsync(
        string masterToken,
        MercadoLivreCatalogSearchRequest request,
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ScanCatalogAsync(request, query, masterToken, affiliateId: null, cancellationToken);
            _logger.LogInformation("[ML Busca] Busca '{Query}' realizada com o token Master (Integrações Globais).", query);
            return result;
        }
        catch (MercadoLivreSearchForbiddenException ex)
        {
            _logger.LogWarning(
                "[ML Busca] API recusou o token Master para '{Query}' ({Reason}); renovando via refresh_token e repetindo a busca.",
                query,
                ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return TraceSearchError(ex, query, cancellationToken);
        }

        var renewed = await RefreshMasterTokenAsync(masterToken, cancellationToken);
        if (renewed is null)
        {
            return null;
        }

        try
        {
            var result = await ScanCatalogAsync(request, query, renewed, affiliateId: null, cancellationToken);
            _logger.LogInformation("[ML Busca] Busca '{Query}' repetida com o token Master renovado.", query);
            return result;
        }
        catch (MercadoLivreSearchForbiddenException ex)
        {
            _logger.LogWarning(
                "[ML Busca] API recusou também o token Master renovado para '{Query}' ({Reason}).",
                query,
                ex.Message);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return TraceSearchError(ex, query, cancellationToken);
        }
    }

    private OfficialCatalogChannelResult TraceSearchError(Exception ex, string query, CancellationToken cancellationToken)
    {
        if (OfficialCatalogSearchRules.IsHttpTimeout(ex, cancellationToken))
        {
            _logger.LogWarning(
                "[ML Busca] A API do Mercado Livre excedeu o tempo limite ({Timeout}s) na busca '{Query}'.",
                _httpClient.Timeout.TotalSeconds,
                query);
            return OfficialCatalogChannelResult.Failed(OfficialCatalogSearchRules.MercadoLivreTimeoutMessage);
        }

        var tracedError = string.IsNullOrWhiteSpace(ex.Message)
            ? OfficialCatalogSearchRules.MercadoLivreSearchForbiddenMessage
            : ex.Message;
        _logger.LogError(ex, "Erro na busca API do Mercado Livre para '{Query}': {Error}", query, tracedError);
        return OfficialCatalogChannelResult.Failed(tracedError);
    }

    private async Task<OfficialCatalogChannelResult> RetryWithRenewedAccountTokenAsync(
        MarketplaceAccount account,
        MercadoLivreCatalogSearchRequest request,
        string query,
        CancellationToken cancellationToken)
    {
        var renewedToken = await RefreshAccountTokenAsync(account, cancellationToken);
        if (renewedToken is null)
        {
            _logger.LogWarning(
                "[ML Busca] Não foi possível renovar o token legado da conta '{AccountName}' (Id={AccountId}): {Hint}.",
                DescribeAccount(account),
                account.Id,
                ReauthorizeMasterHint);
            return OfficialCatalogChannelResult.Failed(OfficialCatalogSearchRules.MercadoLivreSearchForbiddenMessage);
        }

        LogAccountTokenUsage(account, query, renewed: true);
        try
        {
            return await ScanCatalogAsync(request, query, renewedToken, affiliateId: null, cancellationToken);
        }
        catch (Exception ex) when (OfficialCatalogSearchRules.IsHttpTimeout(ex, cancellationToken))
        {
            return TraceSearchError(ex, query, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "[ML Busca] API oficial recusou também o token renovado da conta '{AccountName}' (Id={AccountId}) ({Reason}).",
                DescribeAccount(account),
                account.Id,
                ex.Message);
            return OfficialCatalogChannelResult.Failed(OfficialCatalogSearchRules.MercadoLivreSearchForbiddenMessage);
        }
    }

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

    /// <summary>
    /// <c>/sites/MLB/search</c> foi descontinuado pelo Mercado Livre para apps de terceiros (403 com qualquer token);
    /// a busca usa o buscador de catálogo <c>/products/search</c> e o preço do anúncio ganhador em <c>/products/{id}</c>.
    /// O <c>/products/search</c> não aceita <c>sort</c> nem devolve <c>available_filters</c>: os resultados já vêm na
    /// relevância do Mercado Livre (a ordem é preservada) e as categorias vêm de <c>domain_discovery</c> e dos <c>domain_id</c>.
    /// </summary>
    /// <remarks>
    /// A maioria dos produtos de catálogo não tem anúncio ativo, então a busca varre páginas de
    /// <see cref="OfficialCatalogSearchRules.MercadoLivreScanPageSize"/> até juntar as ofertas pedidas ou atingir
    /// <see cref="OfficialCatalogSearchRules.MercadoLivreMaxScannedProducts"/>; sem nenhuma oferta, tenta termos mais curtos.
    /// </remarks>
    private async Task<OfficialCatalogChannelResult> ScanCatalogAsync(
        MercadoLivreCatalogSearchRequest request,
        string query,
        string accessToken,
        string? affiliateId,
        CancellationToken cancellationToken)
    {
        var target = OfficialCatalogSearchRules.ClampLimit(request.TargetOffers);
        var startOffset = Math.Max(0, request.Offset);
        var candidates = new List<string> { query };
        if (request.AllowShorterQuery && startOffset == 0)
        {
            candidates.AddRange(OfficialCatalogSearchRules.BuildShorterQueries(query));
        }

        var categoriesTask = request.IncludeCategories && startOffset == 0
            ? DiscoverCategoriesAsync(query, accessToken, cancellationToken)
            : Task.FromResult<IReadOnlyList<OfficialCatalogCategoryDto>>([]);

        var items = new List<OfficialCatalogProductDto>();
        var scanned = 0;
        int? nextOffset = null;
        var effectiveQuery = query;
        for (var index = 0; index < candidates.Count && items.Count == 0; index++)
        {
            var remaining = OfficialCatalogSearchRules.MercadoLivreMaxScannedProducts - scanned;
            if (remaining <= 0)
            {
                break;
            }

            var candidate = candidates[index];
            var hasShorterCandidate = index < candidates.Count - 1;
            var offset = startOffset;
            var candidateScanned = 0;
            effectiveQuery = candidate;
            nextOffset = null;
            while (items.Count < target)
            {
                var cap = hasShorterCandidate && items.Count == 0
                    ? Math.Min(OfficialCatalogSearchRules.MercadoLivreScanPerQueryWithFallback, remaining)
                    : remaining;
                if (candidateScanned >= cap)
                {
                    break;
                }

                MercadoLivreCatalogSearchPage? page;
                try
                {
                    page = await FetchCatalogPageAsync(
                        candidate,
                        request.DomainId,
                        offset,
                        accessToken,
                        throwWhenRefused: scanned + candidateScanned == 0,
                        cancellationToken);
                }
                catch (Exception ex) when (items.Count > 0 && OfficialCatalogSearchRules.IsHttpTimeout(ex, cancellationToken))
                {
                    _logger.LogWarning(
                        "[ML Busca] products/search '{Query}' (offset {Offset}) excedeu o tempo limite; mantendo as {OfferCount} ofertas já encontradas.",
                        candidate,
                        offset,
                        items.Count);
                    nextOffset = offset;
                    break;
                }

                if (page is null || page.ProductIds.Count == 0)
                {
                    nextOffset = null;
                    break;
                }

                var productIds = page.ProductIds.Take(cap - candidateScanned).ToList();
                var offers = await ResolveCatalogOffersAsync(productIds, accessToken, cancellationToken);
                foreach (var offer in offers.Where(item => string.IsNullOrWhiteSpace(item.DomainId) && item.ProductId is not null))
                {
                    offer.DomainId = page.DomainByProductId.GetValueOrDefault(offer.ProductId!);
                }

                items.AddRange(offers.Where(item =>
                    OfficialCatalogSearchRules.PassesPriceAndShipping(
                        item,
                        request.MinPrice,
                        request.MaxPrice,
                        request.FreeShippingOnly)
                    && (!request.MainProductsOnly || !OfficialCatalogSearchRules.IsAccessoryOffer(item.ProductName, candidate))));
                candidateScanned += productIds.Count;
                offset += productIds.Count;

                var exhausted = page.ProductIds.Count < OfficialCatalogSearchRules.MercadoLivreScanPageSize
                    || (page.Total is int total && offset >= total);
                nextOffset = exhausted ? null : offset;
                if (exhausted)
                {
                    break;
                }
            }

            scanned += candidateScanned;
        }

        if (items.Count == 0)
        {
            effectiveQuery = query;
            nextOffset = null;
        }

        var finalItems = items
            .Select(item =>
            {
                item.SourceUrl = MercadoLivreCommissionUrlBuilder.InjectMattTool(item.SourceUrl, affiliateId);
                return item;
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.SourceUrl))
            .GroupBy(item => item.ProductId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        if (request.IncludeMetrics)
        {
            await EnrichCatalogMetricsAsync(finalItems, accessToken, cancellationToken);
        }

        await ResolveCategoryNamesAsync(finalItems, accessToken, cancellationToken);
        var categories = OfficialCatalogSearchRules.BuildMercadoLivreCategories(
            await categoriesTask,
            finalItems,
            [request.DomainId],
            DomainNameCache);

        _logger.LogInformation(
            "[ML Busca] products/search '{Query}' (termo usado '{EffectiveQuery}', categoria {DomainId}, offset {Offset}): {Scanned} produtos de catálogo analisados, {OfferCount} ofertas após filtros, próximo offset {NextOffset}.",
            query,
            effectiveQuery,
            string.IsNullOrWhiteSpace(request.DomainId) ? "todas" : request.DomainId.Trim(),
            startOffset,
            scanned,
            finalItems.Count,
            nextOffset?.ToString(CultureInfo.InvariantCulture) ?? "-");

        return new OfficialCatalogChannelResult
        {
            Items = finalItems,
            ScannedCount = scanned,
            NextOffset = nextOffset,
            EffectiveQuery = effectiveQuery,
            Categories = categories
        };
    }

    private async Task<MercadoLivreCatalogSearchPage?> FetchCatalogPageAsync(
        string query,
        string? domainId,
        int offset,
        string accessToken,
        bool throwWhenRefused,
        CancellationToken cancellationToken)
    {
        var searchUri =
            $"https://api.mercadolibre.com/products/search?status=active&site_id=MLB&q={Uri.EscapeDataString(query)}&limit={OfficialCatalogSearchRules.MercadoLivreScanPageSize}";
        if (offset > 0)
        {
            searchUri += $"&offset={offset.ToString(CultureInfo.InvariantCulture)}";
        }

        if (!string.IsNullOrWhiteSpace(domainId))
        {
            searchUri += $"&domain_id={Uri.EscapeDataString(domainId.Trim())}";
        }

        var (statusCode, body) = await SendSearchAsync(searchUri, accessToken, cancellationToken);
        if (statusCode == HttpStatusCode.OK)
        {
            return MercadoLivreItemParser.ParseCatalogSearchPage(body);
        }

        _logger.LogError("Erro API ML [{StatusCode}] em products/search: {Body}", statusCode, Truncate(body));
        if (!throwWhenRefused)
        {
            return null;
        }

        if (OfficialCatalogSearchRules.IsMercadoLivreSearchRefused(statusCode, body))
        {
            throw new MercadoLivreSearchForbiddenException($"HTTP {(int)statusCode}");
        }

        throw new HttpRequestException($"ML HTTP {(int)statusCode} ({statusCode})");
    }

    private async Task<IReadOnlyList<OfficialCatalogProductDto>> ResolveCatalogOffersAsync(
        IReadOnlyList<string> productIds,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var offers = new IReadOnlyList<OfficialCatalogProductDto>[productIds.Count];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, productIds.Count),
            new ParallelOptions { MaxDegreeOfParallelism = CatalogLookupParallelism, CancellationToken = cancellationToken },
            async (index, token) =>
            {
                try
                {
                    offers[index] = await ResolveCatalogOffersAsync(productIds[index], accessToken, followChildren: true, token);
                }
                catch (Exception ex) when (OfficialCatalogSearchRules.IsHttpTimeout(ex, token))
                {
                    _logger.LogWarning(
                        "[ML Busca] Produto de catálogo {ProductId} ignorado: a API do Mercado Livre excedeu o tempo limite.",
                        productIds[index]);
                }
            });
        return offers.SelectMany(productOffers => productOffers ?? []).ToList();
    }

    /// <summary>
    /// Métricas das ofertas encontradas, só pela API oficial: vendas e dados do anúncio (<c>/items</c> em lote),
    /// reputação do vendedor (<c>/users</c>), avaliações (<c>/reviews/item</c>) e ranking de mais vendidos
    /// (<c>/highlights</c>). Falhas deixam a métrica vazia sem afetar a busca.
    /// </summary>
    private async Task EnrichCatalogMetricsAsync(
        IReadOnlyList<OfficialCatalogProductDto> items,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var listingIds = items
            .Select(item => item.ListingId)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (listingIds.Count == 0)
        {
            return;
        }

        foreach (var chunk in listingIds.Chunk(MultigetChunkSize))
        {
            var listings = await FetchOptionalAsync(
                $"items?ids={string.Join(',', chunk.Select(Uri.EscapeDataString))}&attributes=id,sold_quantity,seller_id,official_store_id,warranty,condition,shipping,category_id",
                accessToken,
                MercadoLivreItemParser.ParseItemsMultiget,
                cancellationToken);
            foreach (var item in items)
            {
                if (item.ListingId is null || listings is null || !listings.TryGetValue(item.ListingId, out var listing))
                {
                    continue;
                }

                item.SoldQuantity ??= listing.SoldQuantity;
                item.SellerId ??= listing.SellerId;
                item.IsOfficialStore |= listing.IsOfficialStore;
                item.IsFulfillment |= listing.IsFulfillment;
                item.Warranty ??= listing.Warranty;
                item.Condition ??= listing.Condition;
                item.CategoryId ??= listing.CategoryId;
            }
        }

        var sellerIds = items.Select(item => item.SellerId).OfType<string>().Distinct(StringComparer.Ordinal).ToList();
        var categoryIds = items
            .Select(item => item.CategoryId)
            .OfType<string>()
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .Take(MaxBestSellerCategories)
            .ToList();
        var reviewed = items.Where(item => item.ListingId is not null).ToList();

        var sellersTask = FetchManyAsync(
            sellerIds,
            id => $"users/{Uri.EscapeDataString(id)}",
            MercadoLivreItemParser.ParseSeller,
            accessToken,
            cancellationToken);
        var reviewsTask = FetchManyAsync(
            reviewed.Select(item => item.ListingId!).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            listingId => BuildReviewsPath(
                listingId,
                reviewed.FirstOrDefault(item => string.Equals(item.ListingId, listingId, StringComparison.OrdinalIgnoreCase))?.ProductId),
            MercadoLivreItemParser.ParseReviewSummary,
            accessToken,
            cancellationToken);
        var rankingTask = FetchManyAsync(
            categoryIds,
            id => $"highlights/MLB/category/{Uri.EscapeDataString(id)}",
            MercadoLivreItemParser.ParseBestSellerPositions,
            accessToken,
            cancellationToken);
        var categoryPathsTask = FetchCategoryPathsAsync(
            items.Select(item => item.CategoryId).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            accessToken,
            cancellationToken);
        await Task.WhenAll(sellersTask, reviewsTask, rankingTask, categoryPathsTask);

        var sellers = await sellersTask;
        var reviews = await reviewsTask;
        var rankings = (await rankingTask).Values.ToList();
        var categoryPaths = await categoryPathsTask;
        foreach (var item in items)
        {
            if (item.CategoryId is not null && categoryPaths.TryGetValue(item.CategoryId, out var categoryPath))
            {
                item.CategoryName ??= categoryPath.Name;
                item.RootCategoryId ??= categoryPath.RootId;
                item.RootCategoryName ??= categoryPath.RootName;
            }

            if (item.SellerId is not null && sellers.TryGetValue(item.SellerId, out var seller))
            {
                item.SellerName ??= seller.Nickname;
                item.SellerReputationLevel ??= seller.ReputationLevel;
                item.SellerPowerStatus ??= seller.PowerSellerStatus;
                item.SellerCompletedSales ??= seller.CompletedSales;
                item.SellerPositiveRate ??= seller.PositiveRate;
            }

            if (item.ListingId is not null && reviews.TryGetValue(item.ListingId, out var review))
            {
                item.RatingAverage ??= review.Average;
                item.RatingCount ??= review.Total;
            }

            foreach (var ranking in rankings)
            {
                if ((item.ProductId is not null && ranking.TryGetValue(item.ProductId, out var position))
                    || (item.ListingId is not null && ranking.TryGetValue(item.ListingId, out position)))
                {
                    item.BestSellerPosition = Math.Min(item.BestSellerPosition ?? int.MaxValue, position);
                }
            }
        }

        _logger.LogInformation(
            "[ML Busca] Métricas: {Sellers} vendedores, {Reviews} anúncios com avaliações, {Ranked} no ranking de mais vendidos, {Sold} com vendas informadas, {Categorized} com categoria raiz.",
            sellers.Count,
            reviews.Count,
            items.Count(item => item.BestSellerPosition is not null),
            items.Count(item => item.SoldQuantity is not null),
            items.Count(item => item.RootCategoryId is not null));
    }

    /// <summary><c>/categories/{id}</c> só para as categorias ainda fora do cache (a árvore do ML muda raramente).</summary>
    private async Task<IReadOnlyDictionary<string, MercadoLivreCategoryPath>> FetchCategoryPathsAsync(
        IReadOnlyList<string> categoryIds,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var missing = categoryIds
            .Where(id => !CategoryPathCache.TryGetValue(id, out var cached) || cached.ExpiresAt <= now)
            .Take(MaxCategoryLookups)
            .ToList();
        if (missing.Count > 0)
        {
            var fetched = await FetchManyAsync(
                missing,
                id => $"categories/{Uri.EscapeDataString(id)}",
                MercadoLivreItemParser.ParseCategory,
                accessToken,
                cancellationToken);
            foreach (var (id, path) in fetched)
            {
                CategoryPathCache[id] = (path, now.Add(CategoryPathCacheDuration));
            }
        }

        var result = new Dictionary<string, MercadoLivreCategoryPath>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in categoryIds)
        {
            if (CategoryPathCache.TryGetValue(id, out var cached))
            {
                result[id] = cached.Path;
            }
        }

        return result;
    }

    private static readonly ConcurrentDictionary<string, (MercadoLivreCategoryPath Path, DateTime ExpiresAt)> CategoryPathCache =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly TimeSpan CategoryPathCacheDuration = TimeSpan.FromHours(12);

    private const int MaxCategoryLookups = 20;

    private static string BuildReviewsPath(string listingId, string? productId) =>
        string.IsNullOrWhiteSpace(productId) || string.Equals(productId, listingId, StringComparison.OrdinalIgnoreCase)
            ? $"reviews/item/{Uri.EscapeDataString(listingId)}"
            : $"reviews/item/{Uri.EscapeDataString(listingId)}?catalog_product_id={Uri.EscapeDataString(productId)}";

    private async Task<IReadOnlyDictionary<string, T>> FetchManyAsync<T>(
        IReadOnlyList<string> keys,
        Func<string, string> pathFor,
        Func<string, T?> parse,
        string accessToken,
        CancellationToken cancellationToken)
        where T : class
    {
        var results = new ConcurrentDictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        await Parallel.ForEachAsync(
            keys,
            new ParallelOptions { MaxDegreeOfParallelism = CatalogLookupParallelism, CancellationToken = cancellationToken },
            async (key, token) =>
            {
                var parsed = await FetchOptionalAsync(pathFor(key), accessToken, parse, token);
                if (parsed is not null)
                {
                    results[key] = parsed;
                }
            });
        return results;
    }

    private async Task<T?> FetchOptionalAsync<T>(
        string relativePath,
        string accessToken,
        Func<string, T?> parse,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var (statusCode, body) = await SendApiPathAsync(relativePath, accessToken, cancellationToken);
            if (statusCode == HttpStatusCode.OK)
            {
                return parse(body);
            }

            _logger.LogDebug("[ML Busca] Métrica indisponível [{StatusCode}] em {Path}.", (int)statusCode, relativePath);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "[ML Busca] Falha ao consultar a métrica {Path}.", relativePath);
        }

        return null;
    }

    private const int MultigetChunkSize = 20;
    private const int MaxBestSellerCategories = 5;

    /// <summary>Sugestões de categoria (domínios de catálogo) para o termo; falhas não afetam a busca.</summary>
    private async Task<IReadOnlyList<OfficialCatalogCategoryDto>> DiscoverCategoriesAsync(
        string query,
        string accessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var (statusCode, body) = await SendApiPathAsync(
                $"sites/MLB/domain_discovery/search?limit=6&q={Uri.EscapeDataString(query)}",
                accessToken,
                cancellationToken);
            if (statusCode == HttpStatusCode.OK)
            {
                var discovered = MercadoLivreItemParser.ParseDomainDiscovery(body);
                foreach (var category in discovered.Where(category => !string.Equals(category.Name, category.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    DomainNameCache[category.Id] = category.Name;
                }

                return discovered;
            }

            _logger.LogDebug("[ML Busca] domain_discovery [{StatusCode}] para '{Query}'.", (int)statusCode, query);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "[ML Busca] domain_discovery indisponível para '{Query}'.", query);
        }

        return [];
    }

    /// <summary>Nomes em português dos domínios já vistos no <c>domain_discovery</c>, reaproveitados nas buscas seguintes.</summary>
    private static readonly ConcurrentDictionary<string, string> DomainNameCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Categoria real de cada oferta (<c>category_id</c> do anúncio ➔ <c>/categories/{id}</c>, em português) para nomear
    /// o combo "Categorias" mesmo quando as métricas não foram pedidas; usa o cache de 12 h da árvore de categorias.
    /// </summary>
    private async Task ResolveCategoryNamesAsync(
        IReadOnlyList<OfficialCatalogProductDto> items,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var pending = items
            .Where(item => string.IsNullOrWhiteSpace(item.CategoryName) && !string.IsNullOrWhiteSpace(item.CategoryId))
            .ToList();
        if (pending.Count == 0)
        {
            return;
        }

        var paths = await FetchCategoryPathsAsync(
            pending.Select(item => item.CategoryId!).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            accessToken,
            cancellationToken);
        foreach (var item in pending)
        {
            if (paths.TryGetValue(item.CategoryId!, out var path))
            {
                item.CategoryName ??= path.Name;
                item.RootCategoryId ??= path.RootId;
                item.RootCategoryName ??= path.RootName;
            }
        }
    }

    private const int CatalogLookupParallelism = 8;

    /// <summary>
    /// Ofertas de um produto de catálogo, só pela API oficial: <c>buy_box_winner</c> ➔ faixa de preço da buy box ➔
    /// menor preço em <c>/products/{id}/items</c> ➔ variações (<c>children_ids</c>, até
    /// <see cref="OfficialCatalogSearchRules.MercadoLivreMaxChildrenPerProduct"/>), pois produtos "pai" não são compráveis.
    /// </summary>
    private async Task<IReadOnlyList<OfficialCatalogProductDto>> ResolveCatalogOffersAsync(
        string productId,
        string accessToken,
        bool followChildren,
        CancellationToken cancellationToken)
    {
        var (productStatus, productBody) = await SendApiAsync("products", productId, accessToken, cancellationToken);
        if (productStatus != HttpStatusCode.OK)
        {
            _logger.LogDebug(
                "[ML Busca] Produto de catálogo {ProductId} ignorado: /products [{StatusCode}] {Body}",
                productId,
                (int)productStatus,
                Truncate(productBody));
            return [];
        }

        var offer = MercadoLivreItemParser.ParseCatalogOffer(productBody);
        if (offer is not null)
        {
            return [offer];
        }

        var (itemsStatus, itemsBody) = await SendApiPathAsync(
            $"products/{Uri.EscapeDataString(productId)}/items",
            accessToken,
            cancellationToken);
        if (itemsStatus == HttpStatusCode.OK)
        {
            offer = MercadoLivreItemParser.ParseCatalogItemsOffer(productBody, itemsBody);
            if (offer is not null)
            {
                return [offer];
            }
        }

        var children = MercadoLivreItemParser.ParseCatalogChildrenIds(productBody);
        if (followChildren && children.Count > 0)
        {
            var childOffers = new List<OfficialCatalogProductDto>();
            foreach (var childId in children.Take(OfficialCatalogSearchRules.MercadoLivreMaxChildrenPerProduct))
            {
                childOffers.AddRange(await ResolveCatalogOffersAsync(childId, accessToken, followChildren: false, cancellationToken));
            }

            if (childOffers.Count > 0)
            {
                var parentDomain = MercadoLivreItemParser.ParseCatalogDomainId(productBody);
                foreach (var childOffer in childOffers.Where(item => string.IsNullOrWhiteSpace(item.DomainId)))
                {
                    childOffer.DomainId = parentDomain;
                }

                return childOffers;
            }
        }

        _logger.LogDebug(
            "[ML Busca] Produto de catálogo {ProductId} sem oferta: sem buy_box_winner/faixa de preço, /items [{ItemsStatus}] {ItemsBody}, filhos={ChildCount}.",
            productId,
            (int)itemsStatus,
            Truncate(itemsBody),
            children.Count);
        return [];
    }

    private static string Truncate(string body) => body.Length > 300 ? body[..300] : body;

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

    /// <summary>
    /// Token Master (Painel Master ➔ Integrações Globais): válido é usado direto; vencido é renovado via refresh_token.
    /// Null quando não há autorização Master ou a última renovação falhou (exige reautorizar no Master).
    /// </summary>
    private async Task<string?> ResolveMasterAccessTokenAsync(CancellationToken cancellationToken)
    {
        var token = await LoadMasterTokenAsync(cancellationToken);
        if (token is null)
        {
            return null;
        }

        if (GlobalIntegrationRules.IsAccessTokenFresh(token, DateTime.UtcNow))
        {
            return token.AccessToken.Trim();
        }

        return string.IsNullOrWhiteSpace(token.LastError)
            ? await RefreshMasterTokenAsync(token.AccessToken, cancellationToken)
            : null;
    }

    private async Task<GlobalIntegrationToken?> LoadMasterTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var token = await dbContext.GlobalIntegrationTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Provider == MarketplaceType.MercadoLivre, cancellationToken);
            return token is null
                   || string.IsNullOrWhiteSpace(token.AccessToken)
                   || HomologMarketplaceAuth.IsStubToken(token.AccessToken)
                ? null
                : token;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "[ML Master] Não foi possível ler o token Master da API Mercado Livre.");
            return null;
        }
    }

    /// <summary>
    /// Renova o token Master via refresh_token e grava no banco. Serializado: requisições simultâneas que
    /// encontram o token já renovado por outra reutilizam o novo valor (o refresh_token do ML é de uso único).
    /// </summary>
    private async Task<string?> RefreshMasterTokenAsync(string staleAccessToken, CancellationToken cancellationToken)
    {
        await MasterRefreshLock.WaitAsync(cancellationToken);
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var token = await dbContext.GlobalIntegrationTokens
                .FirstOrDefaultAsync(item => item.Provider == MarketplaceType.MercadoLivre, cancellationToken);
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

            var options = _options.CurrentValue;
            if (!GlobalIntegrationRules.CanRefresh(token, now) || HomologMarketplaceAuth.IsStubToken(token.RefreshToken))
            {
                await RecordMasterErrorAsync(dbContext, token, "Token Master expirado sem refresh_token válido: reautorize no Painel Master.", cancellationToken);
                return null;
            }

            if (!options.HasAppCredentials)
            {
                await RecordMasterErrorAsync(dbContext, token, "AppId/SecretKey do Mercado Livre ausentes no cofre: não é possível renovar o token Master.", cancellationToken);
                return null;
            }

            var refreshed = await RequestOAuthTokenAsync(
                "refresh_token",
                options.AppId.Trim(),
                options.SecretKey.Trim(),
                token.RefreshToken,
                cancellationToken);
            if (string.IsNullOrWhiteSpace(refreshed?.AccessToken))
            {
                await RecordMasterErrorAsync(dbContext, token, "O Mercado Livre recusou o refresh_token Master: reautorize no Painel Master.", cancellationToken);
                return null;
            }

            token.AccessToken = refreshed.AccessToken.Trim();
            if (!string.IsNullOrWhiteSpace(refreshed.RefreshToken))
            {
                token.RefreshToken = refreshed.RefreshToken.Trim();
            }

            token.ExpiresAt = now.AddSeconds(refreshed.ExpiresIn is > 0 ? refreshed.ExpiresIn.Value : 21600);
            token.LastRefreshAt = now;
            token.LastError = null;
            token.Touch();
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "[ML Master] Token Master renovado automaticamente via refresh_token; válido até {ExpiresAt:u}.",
                token.ExpiresAt);
            return token.AccessToken;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "[ML Master] Falha ao renovar o token Master da API Mercado Livre.");
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
        _logger.LogWarning("[ML Master] {Error}", error);
        token.LastError = GlobalIntegrationRules.TruncateError(error);
        token.Touch();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

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

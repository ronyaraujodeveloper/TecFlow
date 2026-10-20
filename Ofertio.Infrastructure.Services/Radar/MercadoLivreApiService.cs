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

    public MercadoLivreApiService(
        HttpClient httpClient,
        IOptionsMonitor<MercadoLivreIntegrationOptions> options,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ILogger<MercadoLivreApiService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
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
                IncludeCategories = false
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
        var accountToken = await ResolveAccountAccessTokenAsync(account, cancellationToken);
        var accessToken = accountToken
            ?? ResolveStaticAccessToken()
            ?? await GetAppAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogWarning(
                "[ML Busca] Sem access_token para buscar '{Query}': conecte a conta Mercado Livre em Minhas Lojas ou configure Integrations:MercadoLivre:AppId/SecretKey.",
                query);
            return OfficialCatalogChannelResult.Unconfigured();
        }

        if (accountToken is not null)
        {
            LogAccountTokenUsage(account!, query, renewed: false);
        }

        try
        {
            return await ScanCatalogAsync(request, query, accessToken.Trim(), affiliateId, cancellationToken);
        }
        catch (MercadoLivreSearchForbiddenException ex) when (accountToken is not null)
        {
            _logger.LogWarning(
                "[ML Busca] API MLB recusou o token da conta '{AccountName}' (Id={AccountId}) para '{Query}' ({Reason}); renovando via refresh_token.",
                DescribeAccount(account!),
                account!.Id,
                query,
                ex.Message);
            return await RetryWithRenewedAccountTokenAsync(account, request, query, affiliateId, cancellationToken);
        }
        catch (MercadoLivreSearchForbiddenException ex)
        {
            _logger.LogWarning(
                "[ML Busca] API oficial recusou a busca de '{Query}' com o token do aplicativo ({Reason}); conecte a conta Mercado Livre em Minhas Lojas (OAuth).",
                query,
                ex.Message);
            EvictAppToken(accessToken);
            return OfficialCatalogChannelResult.Failed(OfficialCatalogSearchRules.MercadoLivreSearchForbiddenMessage);
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

    private async Task<OfficialCatalogChannelResult> RetryWithRenewedAccountTokenAsync(
        MarketplaceAccount account,
        MercadoLivreCatalogSearchRequest request,
        string query,
        string? affiliateId,
        CancellationToken cancellationToken)
    {
        var renewedToken = await RefreshAccountTokenAsync(account, cancellationToken);
        if (renewedToken is null)
        {
            _logger.LogWarning(
                "[ML Busca] Não foi possível renovar o token da conta '{AccountName}' (Id={AccountId}). Reconecte a conta Mercado Livre em Minhas Lojas.",
                DescribeAccount(account),
                account.Id);
            return OfficialCatalogChannelResult.Failed(OfficialCatalogSearchRules.MercadoLivreSearchForbiddenMessage);
        }

        LogAccountTokenUsage(account, query, renewed: true);
        try
        {
            return await ScanCatalogAsync(request, query, renewedToken, affiliateId, cancellationToken);
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
        var seenDomains = new List<string>();
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

                var page = await FetchCatalogPageAsync(
                    candidate,
                    request.DomainId,
                    offset,
                    accessToken,
                    throwWhenRefused: scanned + candidateScanned == 0,
                    cancellationToken);
                if (page is null || page.ProductIds.Count == 0)
                {
                    nextOffset = null;
                    break;
                }

                seenDomains.AddRange(page.DomainIds);
                var productIds = page.ProductIds.Take(cap - candidateScanned).ToList();
                var offers = await ResolveCatalogOffersAsync(productIds, accessToken, cancellationToken);
                items.AddRange(offers.Where(item => OfficialCatalogSearchRules.PassesPriceAndShipping(
                    item,
                    request.MinPrice,
                    request.MaxPrice,
                    request.FreeShippingOnly)));
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
        var categories = MergeCategories(await categoriesTask, seenDomains, request.DomainId);

        _logger.LogInformation(
            "[ML Busca] products/search '{Query}' (termo usado '{EffectiveQuery}', offset {Offset}): {Scanned} produtos de catálogo analisados, {OfferCount} ofertas após filtros, próximo offset {NextOffset}.",
            query,
            effectiveQuery,
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
                offers[index] = await ResolveCatalogOffersAsync(productIds[index], accessToken, followChildren: true, token));
        return offers.SelectMany(productOffers => productOffers ?? []).ToList();
    }

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
                return MercadoLivreItemParser.ParseDomainDiscovery(body);
            }

            _logger.LogDebug("[ML Busca] domain_discovery [{StatusCode}] para '{Query}'.", (int)statusCode, query);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "[ML Busca] domain_discovery indisponível para '{Query}'.", query);
        }

        return [];
    }

    private static IReadOnlyList<OfficialCatalogCategoryDto> MergeCategories(
        IReadOnlyList<OfficialCatalogCategoryDto> discovered,
        IReadOnlyList<string> seenDomains,
        string? selectedDomainId)
    {
        var categories = discovered.ToList();
        foreach (var domainId in seenDomains
                     .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
                     .OrderByDescending(group => group.Count())
                     .Select(group => group.Key)
                     .Append(selectedDomainId ?? string.Empty))
        {
            if (!string.IsNullOrWhiteSpace(domainId)
                && !categories.Any(item => string.Equals(item.Id, domainId, StringComparison.OrdinalIgnoreCase)))
            {
                categories.Add(new OfficialCatalogCategoryDto
                {
                    Id = domainId.Trim(),
                    Name = OfficialCatalogSearchRules.HumanizeMercadoLivreDomain(domainId)
                });
            }
        }

        return categories.Take(MaxCategorySuggestions).ToList();
    }

    private const int CatalogLookupParallelism = 8;
    private const int MaxCategorySuggestions = 8;

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

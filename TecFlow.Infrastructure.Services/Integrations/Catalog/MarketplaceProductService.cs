using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Catalog;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.Shopee.Payloads;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Integrations.TikTokShop.Payloads;
using TecFlow.Core.Enums;

namespace TecFlow.Infrastructure.Services.Integrations.Catalog;

public class MarketplaceProductService : IMarketplaceProductService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IMarketplaceAuthService _authService;
    private readonly IMarketplaceSignatureService _signatureService;
    private readonly IShopeeIntegrationClient _shopeeClient;
    private readonly ITikTokShopIntegrationClient _tikTokClient;
    private readonly ShopeeIntegrationOptions _shopeeOptions;
    private readonly TikTokShopIntegrationOptions _tikTokOptions;
    private readonly ILogger<MarketplaceProductService> _logger;

    public MarketplaceProductService(
        IMarketplaceAuthService authService,
        IMarketplaceSignatureService signatureService,
        IShopeeIntegrationClient shopeeClient,
        ITikTokShopIntegrationClient tikTokClient,
        IOptions<ShopeeIntegrationOptions> shopeeOptions,
        IOptions<TikTokShopIntegrationOptions> tikTokOptions,
        ILogger<MarketplaceProductService> logger)
    {
        _authService = authService;
        _signatureService = signatureService;
        _shopeeClient = shopeeClient;
        _tikTokClient = tikTokClient;
        _shopeeOptions = shopeeOptions.Value;
        _tikTokOptions = tikTokOptions.Value;
        _logger = logger;
    }

    public async Task<ProductResponseDto> FetchProductsFromPlatformAsync(
        string shopId,
        MarketplaceType type,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shopId))
        {
            return Fail("shopId é obrigatório.");
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        try
        {
            return type switch
            {
                MarketplaceType.Shopee => await FetchShopeeProductsAsync(shopId, page, pageSize, cancellationToken),
                MarketplaceType.TikTokShop => await FetchTikTokProductsAsync(shopId, page, pageSize, cancellationToken),
                _ => Fail($"Marketplace não suportado: {type}.")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao sincronizar catálogo {Marketplace} para loja {ShopId}.", type, shopId);
            return Fail($"Erro ao buscar produtos: {ex.Message}");
        }
    }

    public ProductResponseDto ConvertToInternalProductDto(
        MarketplaceType marketplace,
        ShopeeGetItemBaseInfoResponsePayload shopeePayload,
        IReadOnlyDictionary<long, ShopeeGetModelListResponsePayload>? modelsByItemId = null)
    {
        var products = MarketplaceProductMapper.MapShopee(marketplace, shopeePayload, modelsByItemId);
        return MarketplaceProductMapper.ToProductResponseDto(
            true,
            $"Convertidos {products.Count} registro(s) Shopee.",
            products);
    }

    public ProductResponseDto ConvertToInternalProductDto(
        MarketplaceType marketplace,
        TikTokShopProductsSearchResponsePayload tikTokPayload)
    {
        var products = MarketplaceProductMapper.MapTikTok(marketplace, tikTokPayload);
        return MarketplaceProductMapper.ToProductResponseDto(
            true,
            $"Convertidos {products.Count} registro(s) TikTok Shop.",
            products);
    }

    private async Task<ProductResponseDto> FetchShopeeProductsAsync(
        string shopId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        EnsureShopeeCredentials();
        var accessToken = await _authService.GetValidTokenAsync(shopId, MarketplaceType.Shopee, cancellationToken);
        var offset = (page - 1) * pageSize;

        var listPayload = await GetShopeeItemListAsync(shopId, accessToken, offset, pageSize, cancellationToken);
        if (listPayload?.Item is not { Count: > 0 })
        {
            return MarketplaceProductMapper.ToProductResponseDto(true, "Nenhum produto encontrado na Shopee.", []);
        }

        var itemIds = listPayload.Item.Select(i => i.ItemId).ToList();
        var baseInfo = await GetShopeeItemBaseInfoAsync(shopId, accessToken, itemIds, cancellationToken);
        if (baseInfo is null)
        {
            return Fail("Resposta inválida de get_item_base_info na Shopee.");
        }

        var modelsByItemId = new Dictionary<long, ShopeeGetModelListResponsePayload>();
        foreach (var item in baseInfo.ItemList ?? [])
        {
            if (!item.HasModel)
            {
                continue;
            }

            var models = await GetShopeeModelListAsync(shopId, accessToken, item.ItemId, cancellationToken);
            if (models is not null)
            {
                modelsByItemId[item.ItemId] = models;
            }
        }

        return ConvertToInternalProductDto(MarketplaceType.Shopee, baseInfo, modelsByItemId);
    }

    private async Task<ProductResponseDto> FetchTikTokProductsAsync(
        string shopId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        EnsureTikTokCredentials();
        var accessToken = await _authService.GetValidTokenAsync(shopId, MarketplaceType.TikTokShop, cancellationToken);

        var searchPayload = await SearchTikTokProductsAsync(shopId, accessToken, page, pageSize, cancellationToken);
        if (searchPayload is null)
        {
            return Fail("Resposta inválida de products/search no TikTok Shop.");
        }

        return ConvertToInternalProductDto(MarketplaceType.TikTokShop, searchPayload);
    }

    private async Task<ShopeeGetItemListResponsePayload?> GetShopeeItemListAsync(
        string shopId,
        string accessToken,
        int offset,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var path = _shopeeOptions.GetItemListPath;
        var url = BuildShopeeSignedUrl(path, shopId, accessToken, new Dictionary<string, string?>
        {
            ["offset"] = offset.ToString(),
            ["page_size"] = pageSize.ToString(),
            ["item_status"] = "NORMAL"
        });

        using var response = await _shopeeClient.GetAsync(url, cancellationToken);
        return await DeserializeShopeeAsync<ShopeeGetItemListResponsePayload>(response, cancellationToken);
    }

    private async Task<ShopeeGetItemBaseInfoResponsePayload?> GetShopeeItemBaseInfoAsync(
        string shopId,
        string accessToken,
        IReadOnlyList<long> itemIds,
        CancellationToken cancellationToken)
    {
        var merged = new ShopeeGetItemBaseInfoResponsePayload { ItemList = [] };

        foreach (var batch in itemIds.Chunk(50))
        {
            var path = _shopeeOptions.GetItemBaseInfoPath;
            var url = BuildShopeeSignedUrl(path, shopId, accessToken, new Dictionary<string, string?>
            {
                ["item_id_list"] = string.Join(",", batch)
            });

            using var response = await _shopeeClient.GetAsync(url, cancellationToken);
            var chunk = await DeserializeShopeeAsync<ShopeeGetItemBaseInfoResponsePayload>(response, cancellationToken);
            if (chunk?.ItemList is { Count: > 0 })
            {
                merged.ItemList!.AddRange(chunk.ItemList);
            }
        }

        return merged.ItemList!.Count > 0 ? merged : null;
    }

    private async Task<ShopeeGetModelListResponsePayload?> GetShopeeModelListAsync(
        string shopId,
        string accessToken,
        long itemId,
        CancellationToken cancellationToken)
    {
        var path = _shopeeOptions.GetModelListPath;
        var url = BuildShopeeSignedUrl(path, shopId, accessToken, new Dictionary<string, string?>
        {
            ["item_id"] = itemId.ToString()
        });

        using var response = await _shopeeClient.GetAsync(url, cancellationToken);
        return await DeserializeShopeeAsync<ShopeeGetModelListResponsePayload>(response, cancellationToken);
    }

    private async Task<TikTokShopProductsSearchResponsePayload?> SearchTikTokProductsAsync(
        string shopId,
        string accessToken,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var relativePath = _tikTokOptions.ProductsSearchPath.TrimStart('/');
        var apiPath = relativePath.StartsWith("api/", StringComparison.OrdinalIgnoreCase)
            ? "/" + relativePath
            : "/" + relativePath;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var bodyObject = new
        {
            page_size = pageSize,
            page_number = page
        };
        var bodyJson = JsonSerializer.Serialize(bodyObject, JsonOptions);
        var sign = _signatureService.GenerateTikTokShopSign(
            _tikTokOptions.AppKey,
            _tikTokOptions.AppSecret,
            apiPath,
            timestamp,
            bodyJson);

        var url = AppendQuery(relativePath, new Dictionary<string, string?>
        {
            ["app_key"] = _tikTokOptions.AppKey,
            ["timestamp"] = timestamp.ToString(),
            ["sign"] = sign,
            ["access_token"] = accessToken,
            ["shop_id"] = shopId
        });

        using var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        using var response = await _tikTokClient.PostAsync(url, content, cancellationToken);
        return await DeserializeTikTokAsync<TikTokShopProductsSearchResponsePayload>(response, cancellationToken);
    }

    private string BuildShopeeSignedUrl(
        string relativePath,
        string shopId,
        string accessToken,
        Dictionary<string, string?> extraQuery)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var apiPath = NormalizeShopeePath(relativePath);
        var sign = _signatureService.GenerateShopeeSign(
            _shopeeOptions.PartnerId,
            _shopeeOptions.PartnerKey,
            apiPath,
            timestamp,
            accessToken,
            shopId);

        var query = new Dictionary<string, string?>
        {
            ["partner_id"] = _shopeeOptions.PartnerId,
            ["timestamp"] = timestamp.ToString(),
            ["sign"] = sign,
            ["access_token"] = accessToken,
            ["shop_id"] = shopId
        };

        foreach (var kv in extraQuery)
        {
            query[kv.Key] = kv.Value;
        }

        return AppendQuery(relativePath.TrimStart('/'), query);
    }

    private static string NormalizeShopeePath(string relativePath)
    {
        var path = relativePath.Trim();
        return path.StartsWith("api/v2/", StringComparison.OrdinalIgnoreCase)
            ? "/" + path
            : "/api/v2/" + path.TrimStart('/');
    }

    private static string AppendQuery(string basePath, Dictionary<string, string?> query)
    {
        var separator = basePath.Contains('?') ? '&' : '?';
        var pairs = query
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");

        return $"{basePath}{separator}{string.Join('&', pairs)}";
    }

    private async Task<T?> DeserializeShopeeAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Shopee API HTTP {Status}: {Body}", (int)response.StatusCode, content);
            throw new InvalidOperationException($"Shopee API retornou {(int)response.StatusCode}.");
        }

        var envelope = JsonSerializer.Deserialize<ShopeeProductApiEnvelope<T>>(content, JsonOptions);
        if (envelope is null)
        {
            throw new InvalidOperationException("Resposta Shopee inválida.");
        }

        if (!envelope.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Shopee API erro: {envelope.Error} — {envelope.Message}");
        }

        return envelope.Response;
    }

    private async Task<T?> DeserializeTikTokAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("TikTok Shop API HTTP {Status}: {Body}", (int)response.StatusCode, content);
            throw new InvalidOperationException($"TikTok Shop API retornou {(int)response.StatusCode}.");
        }

        var envelope = JsonSerializer.Deserialize<TikTokShopProductApiEnvelope<T>>(content, JsonOptions);
        if (envelope is null)
        {
            throw new InvalidOperationException("Resposta TikTok Shop inválida.");
        }

        if (!envelope.IsSuccess)
        {
            throw new InvalidOperationException(
                $"TikTok Shop API erro ({envelope.Code}): {envelope.Message}");
        }

        return envelope.Data;
    }

    private void EnsureShopeeCredentials()
    {
        if (string.IsNullOrWhiteSpace(_shopeeOptions.PartnerId) || string.IsNullOrWhiteSpace(_shopeeOptions.PartnerKey))
        {
            throw new InvalidOperationException(
                $"Configure {ShopeeIntegrationOptions.SectionName}:PartnerId e PartnerKey.");
        }
    }

    private void EnsureTikTokCredentials()
    {
        if (string.IsNullOrWhiteSpace(_tikTokOptions.AppKey) || string.IsNullOrWhiteSpace(_tikTokOptions.AppSecret))
        {
            throw new InvalidOperationException(
                $"Configure {TikTokShopIntegrationOptions.SectionName}:AppKey e AppSecret.");
        }
    }

    private static ProductResponseDto Fail(string message) =>
        MarketplaceProductMapper.ToProductResponseDto(false, message, []);
}

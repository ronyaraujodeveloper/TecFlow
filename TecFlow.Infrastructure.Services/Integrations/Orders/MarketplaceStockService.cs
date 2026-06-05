using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Orders;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.Shopee.Payloads;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Integrations.TikTokShop.Payloads;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Enums;

namespace TecFlow.Infrastructure.Services.Integrations.Orders;

public class MarketplaceStockService : IMarketplaceStockService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IProductRepository _productRepository;
    private readonly IMarketplaceAuthService _authService;
    private readonly IMarketplaceSignatureService _signatureService;
    private readonly IShopeeIntegrationClient _shopeeClient;
    private readonly ITikTokShopIntegrationClient _tikTokClient;
    private readonly ShopeeIntegrationOptions _shopeeOptions;
    private readonly TikTokShopIntegrationOptions _tikTokOptions;
    private readonly StockConcurrencyGate _stockGate;
    private readonly ILogger<MarketplaceStockService> _logger;

    public MarketplaceStockService(
        IProductRepository productRepository,
        IMarketplaceAuthService authService,
        IMarketplaceSignatureService signatureService,
        IShopeeIntegrationClient shopeeClient,
        ITikTokShopIntegrationClient tikTokClient,
        IOptions<ShopeeIntegrationOptions> shopeeOptions,
        IOptions<TikTokShopIntegrationOptions> tikTokOptions,
        StockConcurrencyGate stockGate,
        ILogger<MarketplaceStockService> logger)
    {
        _productRepository = productRepository;
        _authService = authService;
        _signatureService = signatureService;
        _shopeeClient = shopeeClient;
        _tikTokClient = tikTokClient;
        _shopeeOptions = shopeeOptions.Value;
        _tikTokOptions = tikTokOptions.Value;
        _stockGate = stockGate;
        _logger = logger;
    }

    public Task<int?> DeductLocalStockAsync(
        string shopId,
        string externalSkuId,
        int quantity,
        MarketplaceType type,
        CancellationToken cancellationToken = default)
    {
        var key = $"{type}:{shopId}:{externalSkuId}";
        return _stockGate.RunAsync(key, async () =>
        {
            var product = await _productRepository.GetByMarketplaceSkuAsync(shopId, type, externalSkuId);
            if (product is null)
            {
                _logger.LogWarning(
                    "Produto não vinculado para baixa de estoque. Shop={ShopId}, SKU={Sku}, Marketplace={Marketplace}",
                    shopId, externalSkuId, type);
                return (int?)null;
            }

            var newStock = await _productRepository.AdjustStockAsync(product.Id, -quantity, cancellationToken);
            return newStock >= 0 ? newStock : null;
        });
    }

    public Task<bool> UpdatePlatformStockAsync(
        string shopId,
        string externalSkuId,
        int newQuantity,
        MarketplaceType type,
        CancellationToken cancellationToken = default)
    {
        var key = $"{type}:{shopId}:{externalSkuId}:push";
        return _stockGate.RunAsync(key, () => type switch
        {
            MarketplaceType.Shopee => PushShopeeStockAsync(shopId, externalSkuId, newQuantity, cancellationToken),
            MarketplaceType.TikTokShop => PushTikTokStockAsync(shopId, externalSkuId, newQuantity, cancellationToken),
            _ => Task.FromResult(false)
        });
    }

    private async Task<bool> PushShopeeStockAsync(
        string shopId,
        string externalSkuId,
        int newQuantity,
        CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByMarketplaceSkuAsync(shopId, MarketplaceType.Shopee, externalSkuId);
        if (product is null || string.IsNullOrWhiteSpace(product.ExternalProductId))
        {
            return false;
        }

        if (!long.TryParse(product.ExternalProductId, out var itemId))
        {
            var parts = product.ExternalProductId.Split('-', 2);
            if (!long.TryParse(parts[0], out itemId))
            {
                return false;
            }
        }

        long modelId = 0;
        if (product.SkuCode?.Contains('-') == true &&
            long.TryParse(product.SkuCode.Split('-')[^1], out var parsedModel))
        {
            modelId = parsedModel;
        }

        var accessToken = await _authService.GetValidTokenAsync(shopId, MarketplaceType.Shopee, cancellationToken);
        var body = new ShopeeUpdateStockRequestPayload
        {
            ItemId = itemId,
            StockList =
            [
                new ShopeeStockUpdateEntry
                {
                    ModelId = modelId,
                    SellerStock =
                    [
                        new ShopeeSellerStockUpdate { LocationId = string.Empty, Stock = newQuantity }
                    ]
                }
            ]
        };

        var bodyJson = JsonSerializer.Serialize(body, JsonOptions);
        var url = BuildShopeeSignedUrl(_shopeeOptions.UpdateStockPath, shopId, accessToken);
        using var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        using var response = await _shopeeClient.PostAsync(url, content, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> PushTikTokStockAsync(
        string shopId,
        string externalSkuId,
        int newQuantity,
        CancellationToken cancellationToken)
    {
        var accessToken = await _authService.GetValidTokenAsync(shopId, MarketplaceType.TikTokShop, cancellationToken);
        var relativePath = _tikTokOptions.UpdateStocksPath.TrimStart('/');
        var apiPath = relativePath.StartsWith("api/", StringComparison.OrdinalIgnoreCase)
            ? "/" + relativePath
            : "/" + relativePath;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var body = new TikTokShopUpdateStocksRequestPayload
        {
            Skus =
            [
                new TikTokShopStockUpdateSku
                {
                    SkuId = externalSkuId,
                    Inventory =
                    [
                        new TikTokShopSkuInventory { Quantity = newQuantity, AvailableStock = newQuantity }
                    ]
                }
            ]
        };

        var bodyJson = JsonSerializer.Serialize(body, JsonOptions);
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
        return response.IsSuccessStatusCode;
    }

    private string BuildShopeeSignedUrl(string relativePath, string shopId, string accessToken)
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

        return AppendQuery(relativePath.TrimStart('/'), new Dictionary<string, string?>
        {
            ["partner_id"] = _shopeeOptions.PartnerId,
            ["timestamp"] = timestamp.ToString(),
            ["sign"] = sign,
            ["access_token"] = accessToken,
            ["shop_id"] = shopId
        });
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
}

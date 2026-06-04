using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Orders;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.Shopee.Payloads;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Integrations.TikTokShop.Payloads;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Infrastructure.Services.Integrations.Orders;

public class MarketplaceOrderService : IMarketplaceOrderService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> ShopeeDeductStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "READY_TO_SHIP", "PROCESSED", "SHIPPED", "COMPLETED", "TO_CONFIRM_RECEIVE"
    };

    private static readonly HashSet<string> TikTokDeductStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "AWAITING_SHIPMENT", "AWAITING_COLLECTION", "IN_TRANSIT", "DELIVERED", "COMPLETED"
    };

    private readonly IMarketplaceOrderRepository _orderRepository;
    private readonly IMarketplaceStockService _stockService;
    private readonly IMarketplaceAuthService _authService;
    private readonly IMarketplaceSignatureService _signatureService;
    private readonly IShopeeIntegrationClient _shopeeClient;
    private readonly ITikTokShopIntegrationClient _tikTokClient;
    private readonly ShopeeIntegrationOptions _shopeeOptions;
    private readonly TikTokShopIntegrationOptions _tikTokOptions;
    private readonly ILogger<MarketplaceOrderService> _logger;

    public MarketplaceOrderService(
        IMarketplaceOrderRepository orderRepository,
        IMarketplaceStockService stockService,
        IMarketplaceAuthService authService,
        IMarketplaceSignatureService signatureService,
        IShopeeIntegrationClient shopeeClient,
        ITikTokShopIntegrationClient tikTokClient,
        IOptions<ShopeeIntegrationOptions> shopeeOptions,
        IOptions<TikTokShopIntegrationOptions> tikTokOptions,
        ILogger<MarketplaceOrderService> logger)
    {
        _orderRepository = orderRepository;
        _stockService = stockService;
        _authService = authService;
        _signatureService = signatureService;
        _shopeeClient = shopeeClient;
        _tikTokClient = tikTokClient;
        _shopeeOptions = shopeeOptions.Value;
        _tikTokOptions = tikTokOptions.Value;
        _logger = logger;
    }

    public async Task<MarketplaceOrderResult> ProcessWebhookOrderAsync(
        string rawJson,
        MarketplaceType type,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return type switch
            {
                MarketplaceType.Shopee => await ProcessShopeeWebhookAsync(rawJson, cancellationToken),
                MarketplaceType.TikTokShop => await ProcessTikTokWebhookAsync(rawJson, cancellationToken),
                _ => Fail($"Marketplace não suportado: {type}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar webhook {Marketplace}.", type);
            return Fail(ex.Message);
        }
    }

    public async Task<MarketplaceOrderResult> SyncMissingOrdersPollingAsync(
        string shopId,
        MarketplaceType type,
        int hoursBack = 24,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shopId))
        {
            return Fail("shopId é obrigatório.");
        }

        hoursBack = Math.Clamp(hoursBack, 1, 168);
        var processed = 0;
        var skipped = 0;

        try
        {
            var candidates = type switch
            {
                MarketplaceType.Shopee => await PollShopeeOrdersAsync(shopId, hoursBack, cancellationToken),
                MarketplaceType.TikTokShop => await PollTikTokOrdersAsync(shopId, hoursBack, cancellationToken),
                _ => []
            };

            foreach (var candidate in candidates)
            {
                if (await _orderRepository.ExistsAsync(candidate.ExternalOrderId, shopId, type))
                {
                    skipped++;
                    continue;
                }

                var result = await PersistOrderAsync(
                    shopId,
                    type,
                    candidate.ExternalOrderId,
                    candidate.Status,
                    candidate.Lines,
                    cancellationToken);

                if (result.Status)
                {
                    processed++;
                }
            }

            return new MarketplaceOrderResult
            {
                Status = true,
                Descricao = $"Polling concluído: {processed} processado(s), {skipped} já existente(s).",
                LinesProcessed = processed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Polling de pedidos falhou para {Marketplace} loja {ShopId}.", type, shopId);
            return Fail(ex.Message);
        }
    }

    private async Task<MarketplaceOrderResult> ProcessShopeeWebhookAsync(
        string rawJson,
        CancellationToken cancellationToken)
    {
        var push = JsonSerializer.Deserialize<ShopeePushNotificationPayload>(rawJson, JsonOptions);
        if (push?.Data is null)
        {
            return Fail("Payload Shopee inválido.");
        }

        var orderSn = push.Data.ResolveOrderSn();
        if (string.IsNullOrWhiteSpace(orderSn))
        {
            return Fail("order_sn ausente no webhook Shopee.");
        }

        var shopId = push.ShopId.ToString();
        var status = push.Data.Status ?? string.Empty;

        if (await _orderRepository.ExistsAsync(orderSn, shopId, MarketplaceType.Shopee))
        {
            return Idempotent(orderSn, "Pedido Shopee já processado (idempotência).");
        }

        var lines = await FetchShopeeOrderLinesAsync(shopId, orderSn, cancellationToken);
        return await PersistOrderAsync(shopId, MarketplaceType.Shopee, orderSn, status, lines, cancellationToken);
    }

    private async Task<MarketplaceOrderResult> ProcessTikTokWebhookAsync(
        string rawJson,
        CancellationToken cancellationToken)
    {
        var push = JsonSerializer.Deserialize<TikTokShopWebhookPayload>(rawJson, JsonOptions);
        if (push?.Data is null)
        {
            return Fail("Payload TikTok Shop inválido.");
        }

        var orderId = push.Data.OrderId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return Fail("order_id ausente no webhook TikTok Shop.");
        }

        var shopId = push.ShopId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(shopId))
        {
            return Fail("shop_id ausente no webhook TikTok Shop.");
        }

        var status = push.Data.OrderStatus ?? string.Empty;

        if (await _orderRepository.ExistsAsync(orderId, shopId, MarketplaceType.TikTokShop))
        {
            return Idempotent(orderId, "Pedido TikTok Shop já processado (idempotência).");
        }

        var lines = await FetchTikTokOrderLinesAsync(shopId, orderId, status, cancellationToken);
        return await PersistOrderAsync(shopId, MarketplaceType.TikTokShop, orderId, status, lines, cancellationToken);
    }

    private async Task<MarketplaceOrderResult> PersistOrderAsync(
        string shopId,
        MarketplaceType type,
        string externalOrderId,
        string status,
        IReadOnlyList<MarketplaceOrderLineData> lines,
        CancellationToken cancellationToken)
    {
        var shouldDeduct = ShouldDeductStock(type, status);
        var deducted = false;
        var linesProcessed = 0;

        if (shouldDeduct)
        {
            foreach (var line in lines)
            {
                if (line.Quantity <= 0 || string.IsNullOrWhiteSpace(line.SkuCode))
                {
                    continue;
                }

                var newStock = await _stockService.DeductLocalStockAsync(
                    shopId, line.SkuCode, line.Quantity, type, cancellationToken);

                if (newStock.HasValue)
                {
                    linesProcessed++;
                    await _stockService.UpdatePlatformStockAsync(
                        shopId, line.SkuCode, newStock.Value, type, cancellationToken);
                }
            }

            deducted = linesProcessed > 0;
        }

        var order = new MarketplaceOrder
        {
            ExternalOrderId = externalOrderId,
            ShopId = shopId,
            MarketplaceType = type,
            Status = status,
            StockDeducted = deducted,
            Lines = lines.Select(l => new MarketplaceOrderLine
            {
                SkuCode = l.SkuCode,
                ExternalSkuId = l.SkuCode,
                ExternalProductId = l.ExternalProductId,
                Quantity = l.Quantity
            }).ToList()
        };

        try
        {
            await _orderRepository.CreateAsync(order);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Concorrência/idempotência ao persistir pedido {OrderId}.", externalOrderId);
            return Idempotent(externalOrderId, "Pedido já persistido por processo concorrente.");
        }

        return new MarketplaceOrderResult
        {
            Status = true,
            Descricao = deducted
                ? $"Pedido processado com baixa de estoque em {linesProcessed} linha(s)."
                : "Pedido registrado sem baixa de estoque (status não elegível ou SKU não vinculado).",
            ExternalOrderId = externalOrderId,
            LinesProcessed = linesProcessed
        };
    }

    private static bool ShouldDeductStock(MarketplaceType type, string status) =>
        type switch
        {
            MarketplaceType.Shopee => ShopeeDeductStatuses.Contains(status),
            MarketplaceType.TikTokShop => TikTokDeductStatuses.Contains(status),
            _ => false
        };

    private async Task<List<MarketplaceOrderLineData>> FetchShopeeOrderLinesAsync(
        string shopId,
        string orderSn,
        CancellationToken cancellationToken)
    {
        var accessToken = await _authService.GetValidTokenAsync(shopId, MarketplaceType.Shopee, cancellationToken);
        var url = BuildShopeeSignedUrl(
            _shopeeOptions.GetOrderDetailPath,
            shopId,
            accessToken,
            new Dictionary<string, string?>
            {
                ["order_sn_list"] = orderSn,
                ["response_optional_fields"] = "item_list"
            });

        using var response = await _shopeeClient.GetAsync(url, cancellationToken);
        var detail = await DeserializeShopeeAsync<ShopeeGetOrderDetailResponsePayload>(response, cancellationToken);
        var order = detail?.OrderList?.FirstOrDefault();
        if (order?.ItemList is null)
        {
            return [];
        }

        return order.ItemList
            .Select(i => new MarketplaceOrderLineData(
                i.ResolveSku(),
                i.ItemId.ToString(),
                i.ModelQuantityPurchased))
            .ToList();
    }

    private async Task<List<MarketplaceOrderLineData>> FetchTikTokOrderLinesAsync(
        string shopId,
        string orderId,
        string status,
        CancellationToken cancellationToken)
    {
        var polled = await PollTikTokOrdersAsync(shopId, 48, cancellationToken);
        var match = polled.FirstOrDefault(o => o.ExternalOrderId == orderId);
        if (match is not null && match.Lines.Count > 0)
        {
            return match.Lines;
        }

        return
        [
            new MarketplaceOrderLineData(orderId, orderId, 1)
        ];
    }

    private async Task<List<PollOrderCandidate>> PollShopeeOrdersAsync(
        string shopId,
        int hoursBack,
        CancellationToken cancellationToken)
    {
        var accessToken = await _authService.GetValidTokenAsync(shopId, MarketplaceType.Shopee, cancellationToken);
        var timeTo = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timeFrom = timeTo - (hoursBack * 3600);

        var url = BuildShopeeSignedUrl(
            _shopeeOptions.GetOrderListPath,
            shopId,
            accessToken,
            new Dictionary<string, string?>
            {
                ["time_range_field"] = "update_time",
                ["time_from"] = timeFrom.ToString(),
                ["time_to"] = timeTo.ToString(),
                ["page_size"] = "50"
            });

        using var response = await _shopeeClient.GetAsync(url, cancellationToken);
        var list = await DeserializeShopeeAsync<ShopeeGetOrderListResponsePayload>(response, cancellationToken);
        if (list?.OrderList is null)
        {
            return [];
        }

        var results = new List<PollOrderCandidate>();
        foreach (var entry in list.OrderList)
        {
            if (string.IsNullOrWhiteSpace(entry.OrderSn))
            {
                continue;
            }

            var lines = await FetchShopeeOrderLinesAsync(shopId, entry.OrderSn, cancellationToken);
            results.Add(new PollOrderCandidate(
                entry.OrderSn,
                entry.OrderStatus ?? string.Empty,
                lines));
        }

        return results;
    }

    private async Task<List<PollOrderCandidate>> PollTikTokOrdersAsync(
        string shopId,
        int hoursBack,
        CancellationToken cancellationToken)
    {
        var accessToken = await _authService.GetValidTokenAsync(shopId, MarketplaceType.TikTokShop, cancellationToken);
        var relativePath = _tikTokOptions.OrdersSearchPath.TrimStart('/');
        var apiPath = "/" + relativePath;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var updateFrom = timestamp - (hoursBack * 3600L);

        var bodyObject = new
        {
            page_size = 50,
            page_number = 1,
            update_time_from = updateFrom,
            update_time_to = timestamp
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
        var payload = await DeserializeTikTokAsync<TikTokShopOrdersSearchResponsePayload>(response, cancellationToken);
        if (payload?.Orders is null)
        {
            return [];
        }

        return payload.Orders
            .Where(o => !string.IsNullOrWhiteSpace(o.ResolveOrderId()))
            .Select(o => new PollOrderCandidate(
                o.ResolveOrderId(),
                o.Status ?? string.Empty,
                (o.LineItems ?? [])
                    .Select(li => new MarketplaceOrderLineData(
                        li.ResolveSku(),
                        li.ProductId,
                        Math.Max(1, li.Quantity)))
                    .ToList()))
            .ToList();
    }

    private string BuildShopeeSignedUrl(
        string relativePath,
        string shopId,
        string accessToken,
        Dictionary<string, string?>? extraQuery = null)
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

        if (extraQuery is not null)
        {
            foreach (var kv in extraQuery)
            {
                query[kv.Key] = kv.Value;
            }
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

    private async Task<T?> DeserializeShopeeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Shopee API HTTP {(int)response.StatusCode}: {content}");
        }

        var envelope = JsonSerializer.Deserialize<ShopeeProductApiEnvelope<T>>(content, JsonOptions);
        if (envelope is null || !envelope.IsSuccess)
        {
            throw new InvalidOperationException($"Shopee API erro: {envelope?.Error} {envelope?.Message}");
        }

        return envelope.Response;
    }

    private async Task<T?> DeserializeTikTokAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"TikTok Shop API HTTP {(int)response.StatusCode}: {content}");
        }

        var envelope = JsonSerializer.Deserialize<TikTokShopProductApiEnvelope<T>>(content, JsonOptions);
        if (envelope is null || !envelope.IsSuccess)
        {
            throw new InvalidOperationException($"TikTok Shop API erro ({envelope?.Code}): {envelope?.Message}");
        }

        return envelope.Data;
    }

    private static MarketplaceOrderResult Fail(string message) =>
        new() { Status = false, Descricao = message };

    private static MarketplaceOrderResult Idempotent(string orderId, string message) =>
        new()
        {
            Status = true,
            Descricao = message,
            ExternalOrderId = orderId,
            AlreadyProcessed = true
        };

    private sealed record MarketplaceOrderLineData(
        string SkuCode,
        string? ExternalProductId,
        int Quantity);

    private sealed record PollOrderCandidate(
        string ExternalOrderId,
        string Status,
        List<MarketplaceOrderLineData> Lines);
}

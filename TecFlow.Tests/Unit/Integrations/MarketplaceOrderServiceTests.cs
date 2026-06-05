using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Orders;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Infrastructure.Services.Integrations.Auth;
using TecFlow.Infrastructure.Services.Integrations.Orders;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.Integrations;

public class MarketplaceOrderServiceTests
{
    private readonly Mock<IMarketplaceOrderRepository> _orderRepository = new();
    private readonly Mock<IMarketplaceStockService> _stockService = new();
    private readonly Mock<IMarketplaceAuthService> _authService = new();
    private readonly Mock<IShopeeIntegrationClient> _shopeeClient = new();
    private readonly Mock<ITikTokShopIntegrationClient> _tikTokClient = new();

    private MarketplaceOrderService CreateService() =>
        new(
            _orderRepository.Object,
            _stockService.Object,
            _authService.Object,
            new MarketplaceSignatureService(),
            _shopeeClient.Object,
            _tikTokClient.Object,
            MarketplaceTestOptionsFactory.ShopeeOptions(),
            MarketplaceTestOptionsFactory.TikTokOptions(),
            NullLogger<MarketplaceOrderService>.Instance);

    [Fact]
    public async Task ProcessWebhookOrderAsync_ShouldReturnFailure_WhenShopeePayloadIsCorrupted()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ProcessWebhookOrderAsync("{invalid", MarketplaceType.Shopee);

        // Assert
        Assert.False(result.Status);
    }

    [Fact]
    public async Task ProcessWebhookOrderAsync_ShouldReturnFailure_WhenShopeePayloadMissingOrderSn()
    {
        // Arrange
        var service = CreateService();
        const string json = """{"code":3,"shop_id":10,"data":{"status":"READY_TO_SHIP"}}""";

        // Act
        var result = await service.ProcessWebhookOrderAsync(json, MarketplaceType.Shopee);

        // Assert
        Assert.False(result.Status);
        Assert.Contains("order_sn", result.Descricao, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessWebhookOrderAsync_ShouldReturnIdempotent_WhenOrderAlreadyExists()
    {
        // Arrange
        var service = CreateService();
        const string json = """{"code":3,"shop_id":10,"data":{"ordersn":"ORD-EXIST","status":"READY_TO_SHIP"}}""";

        _orderRepository
            .Setup(r => r.ExistsAsync("ORD-EXIST", "10", MarketplaceType.Shopee))
            .ReturnsAsync(true);

        // Act
        var result = await service.ProcessWebhookOrderAsync(json, MarketplaceType.Shopee);

        // Assert
        Assert.True(result.Status);
        Assert.True(result.AlreadyProcessed);
        _shopeeClient.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessWebhookOrderAsync_ShouldDeductStock_WhenShopeeOrderIsNew()
    {
        // Arrange
        var service = CreateService();
        const string shopId = "10";
        const string orderSn = "ORD-NEW";
        var webhookJson =
            "{\"code\":3,\"shop_id\":" + shopId + ",\"data\":{\"ordersn\":\"" + orderSn + "\",\"status\":\"READY_TO_SHIP\"}}";

        _orderRepository
            .Setup(r => r.ExistsAsync(orderSn, shopId, MarketplaceType.Shopee))
            .ReturnsAsync(false);

        _authService
            .Setup(a => a.GetValidTokenAsync(shopId, MarketplaceType.Shopee, It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token");

        var orderDetailJson = """
            {
              "error": "",
              "message": "",
              "response": {
                "order_list": [
                  {
                    "order_sn": "ORD-NEW",
                    "order_status": "READY_TO_SHIP",
                    "item_list": [
                      {
                        "item_id": 1,
                        "model_id": 0,
                        "model_sku": "SKU-NEW",
                        "model_quantity_purchased": 2
                      }
                    ]
                  }
                ]
              }
            }
            """;

        _shopeeClient
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(orderDetailJson, System.Text.Encoding.UTF8, "application/json")
            });

        _stockService
            .Setup(s => s.DeductLocalStockAsync(shopId, "SKU-NEW", 2, MarketplaceType.Shopee, It.IsAny<CancellationToken>()))
            .ReturnsAsync(8);

        _stockService
            .Setup(s => s.UpdatePlatformStockAsync(shopId, "SKU-NEW", 8, MarketplaceType.Shopee, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _orderRepository
            .Setup(r => r.CreateAsync(It.IsAny<MarketplaceOrder>()))
            .ReturnsAsync((MarketplaceOrder o) => o);

        // Act
        var result = await service.ProcessWebhookOrderAsync(webhookJson, MarketplaceType.Shopee);

        // Assert
        Assert.True(result.Status);
        Assert.False(result.AlreadyProcessed);
        Assert.Equal(1, result.LinesProcessed);
        _stockService.Verify(s => s.DeductLocalStockAsync(shopId, "SKU-NEW", 2, MarketplaceType.Shopee, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessWebhookOrderAsync_ShouldReturnFailure_WhenTikTokPayloadMissingShopId()
    {
        // Arrange
        var service = CreateService();
        const string json = """{"type":"order_status_change","data":{"order_id":"55","order_status":"AWAITING_SHIPMENT"}}""";

        // Act
        var result = await service.ProcessWebhookOrderAsync(json, MarketplaceType.TikTokShop);

        // Assert
        Assert.False(result.Status);
        Assert.Contains("shop_id", result.Descricao, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SyncMissingOrdersPollingAsync_ShouldReturnFailure_WhenShopIdIsEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SyncMissingOrdersPollingAsync("", MarketplaceType.Shopee);

        // Assert
        Assert.False(result.Status);
    }
}

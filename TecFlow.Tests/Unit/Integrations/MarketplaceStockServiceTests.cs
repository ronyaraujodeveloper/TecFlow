using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Infrastructure.Services.Integrations.Auth;
using TecFlow.Infrastructure.Services.Integrations.Orders;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.Integrations;

public class MarketplaceStockServiceTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IMarketplaceAuthService> _authService = new();
    private readonly Mock<IShopeeIntegrationClient> _shopeeClient = new();
    private readonly Mock<ITikTokShopIntegrationClient> _tikTokClient = new();
    private readonly StockConcurrencyGate _stockGate = new();

    private MarketplaceStockService CreateService() =>
        new(
            _productRepository.Object,
            _authService.Object,
            new MarketplaceSignatureService(),
            _shopeeClient.Object,
            _tikTokClient.Object,
            MarketplaceTestOptionsFactory.ShopeeOptions(),
            MarketplaceTestOptionsFactory.TikTokOptions(),
            _stockGate,
            NullLogger<MarketplaceStockService>.Instance);

    [Fact]
    public async Task DeductLocalStockAsync_ShouldReturnNull_WhenProductIsNotLinked()
    {
        // Arrange
        var service = CreateService();
        _productRepository
            .Setup(r => r.GetByMarketplaceSkuAsync("shop-1", MarketplaceType.Shopee, "SKU-X"))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await service.DeductLocalStockAsync("shop-1", "SKU-X", 1, MarketplaceType.Shopee);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeductLocalStockAsync_ShouldReturnNewStock_WhenProductExists()
    {
        // Arrange
        var service = CreateService();
        _productRepository
            .Setup(r => r.GetByMarketplaceSkuAsync("shop-1", MarketplaceType.Shopee, "SKU-1"))
            .ReturnsAsync(new Product { Id = 5, Stock = 10, SkuCode = "SKU-1" });

        _productRepository
            .Setup(r => r.AdjustStockAsync(5, -3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        // Act
        var result = await service.DeductLocalStockAsync("shop-1", "SKU-1", 3, MarketplaceType.Shopee);

        // Assert
        Assert.Equal(7, result);
    }

    [Fact]
    public async Task UpdatePlatformStockAsync_ShouldReturnFalse_WhenProductHasNoExternalId()
    {
        // Arrange
        var service = CreateService();
        _productRepository
            .Setup(r => r.GetByMarketplaceSkuAsync("shop-1", MarketplaceType.Shopee, "SKU-1"))
            .ReturnsAsync(new Product { Id = 1, SkuCode = "SKU-1", ExternalProductId = null });

        // Act
        var result = await service.UpdatePlatformStockAsync("shop-1", "SKU-1", 5, MarketplaceType.Shopee);

        // Assert
        Assert.False(result);
        _shopeeClient.Verify(c => c.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

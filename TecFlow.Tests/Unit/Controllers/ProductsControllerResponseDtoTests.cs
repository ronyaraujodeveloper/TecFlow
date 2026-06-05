using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.API.Controllers;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Orders;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Application;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database.Filter;

namespace TecFlow.Tests.Unit.Controllers;

public class ProductsControllerResponseDtoTests
{
    private static ProductsController CreateController(
        Mock<IProductRepository> productRepo,
        Mock<IMarketplaceStockService>? stockService = null)
    {
        var aiService = new AIApplicationService(
            new Mock<IAIService>().Object,
            NullLogger<AIApplicationService>.Instance);

        var controller = new ProductsController(
            productRepo.Object,
            (stockService ?? new Mock<IMarketplaceStockService>()).Object,
            NullLogger<ProductsController>.Instance,
            aiService);

        var claims = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "42")]);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(claims) }
        };

        return controller;
    }

    [Fact]
    public async Task GetByFilterAsync_ShouldReturnProductResponseDtoWithDataList_WhenProductsExist()
    {
        // Arrange
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetByOwnerIdAsync(42)).ReturnsAsync(
        [
            new Product { Id = 1, OwnerId = 42, Name = "Produto A", Price = 10, Stock = 5 }
        ]);

        var controller = CreateController(repo);
        var filter = new ProductFilter { Name = "Produto" };

        // Act
        var actionResult = await controller.GetByFilterAsync(filter);
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var dto = Assert.IsType<ProductResponseDto>(ok.Value);

        // Assert
        Assert.True(dto.Status);
        Assert.Equal("OK", dto.Descricao);
        Assert.Single(dto.DataList!);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFoundResponseDto_WhenProductDoesNotExist()
    {
        // Arrange
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Product?)null);
        var controller = CreateController(repo);

        // Act
        var actionResult = await controller.GetByIdAsync(999);
        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        var dto = Assert.IsType<ProductResponseDto>(notFound.Value);

        // Assert
        Assert.False(dto.Status);
        Assert.Null(dto.Data);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPushStockToMarketplace_WhenStockChangesAndSkuIsLinked()
    {
        // Arrange
        var repo = new Mock<IProductRepository>();
        var stock = new Mock<IMarketplaceStockService>();
        var product = new Product
        {
            Id = 7,
            OwnerId = 42,
            Name = "Item",
            Stock = 10,
            SkuCode = "SKU-7",
            MarketplaceShopId = "shop-99",
            MarketplaceSource = MarketplaceType.Shopee
        };

        repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(product);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        stock.Setup(s => s.UpdatePlatformStockAsync("shop-99", "SKU-7", 3, MarketplaceType.Shopee, default))
            .ReturnsAsync(true);

        var controller = CreateController(repo, stock);
        var dto = new ProductDto { Name = "Item", Stock = 3, Price = 1 };

        // Act
        var actionResult = await controller.UpdateAsync(7, dto);

        // Assert
        Assert.IsType<OkObjectResult>(actionResult.Result);
        stock.Verify(s => s.UpdatePlatformStockAsync("shop-99", "SKU-7", 3, MarketplaceType.Shopee, default), Times.Once);
    }
}

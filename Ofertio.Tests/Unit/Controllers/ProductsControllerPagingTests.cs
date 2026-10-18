using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ofertio.API.Controllers;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Orders;
using Ofertio.Business.Interfaces.Repositories;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Application;
using Ofertio.Core.Entities;
using Ofertio.Database.Filter;

namespace Ofertio.Tests.Unit.Controllers;

public class ProductsControllerPagingTests
{
    [Fact]
    public async Task GetByFilterAsync_ShouldReturnPagingMetadata_WhenPageSizeLimited()
    {
        var products = Enumerable.Range(1, 15)
            .Select(i => new Product { Id = i, OwnerId = 42, Name = $"Produto {i}", Price = 10, Stock = 1 })
            .ToList();

        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetByOwnerIdAsync(42)).ReturnsAsync(products);

        var aiService = new AIApplicationService(
            new Mock<IAIService>().Object,
            NullLogger<AIApplicationService>.Instance);

        var controller = new ProductsController(
            repo.Object,
            new Mock<IMarketplaceStockService>().Object,
            NullLogger<ProductsController>.Instance,
            aiService);

        var claims = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "42")]);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(claims) }
        };

        var filter = new ProductFilter { Page = 1, PageSize = 10 };
        var actionResult = await controller.GetByFilterAsync(filter);
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var dto = Assert.IsType<ProductResponseDto>(ok.Value);

        Assert.NotNull(dto.Paging);
        Assert.Equal(15, dto.Paging!.TotalCount);
        Assert.Equal(10, dto.DataList!.Count);
        Assert.True(dto.Paging.HasNextPage);
    }
}

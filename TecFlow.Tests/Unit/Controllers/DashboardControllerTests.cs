using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TecFlow.API.Controllers;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Database.Entity;
using TecFlow.Database.Filter;
using TecFlow.Infrastructure.Services.Services;

namespace TecFlow.Tests.Unit.Controllers;

public class DashboardControllerTests
{
    [Fact]
    public async Task GetDashboardStatsAsync_ShouldReturn401_WhenUserIsMissing()
    {
        var controller = CreateController(new Mock<IAnaliseCalculoService>().Object, new Mock<IIntegracaoLojaRepository>().Object, userId: null);

        var action = await controller.GetDashboardStatsAsync(new DashboardAnalyticsFilter(), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(action.Result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_ShouldReturn400_WhenLojaDoesNotBelongToUser()
    {
        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegracaoLoja { Id = 99, UserId = 2, ShopId = "1", FriendlyName = "Outra" });

        var controller = CreateController(new Mock<IAnaliseCalculoService>().Object, stores.Object);
        var action = await controller.GetDashboardStatsAsync(new DashboardAnalyticsFilter { LojaId = 99 }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(action.Result);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_ShouldReturn500_WhenAnalyticsThrows()
    {
        var analytics = new Mock<IAnaliseCalculoService>();
        analytics.Setup(s => s.CalculateDashboardStatisticsAsync(7, 3))
            .ThrowsAsync(new InvalidOperationException("db"));

        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(s => s.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegracaoLoja { Id = 3, UserId = 7, ShopId = "123", FriendlyName = "Loja" });

        var controller = CreateController(analytics.Object, stores.Object);
        var action = await controller.GetDashboardStatsAsync(new DashboardAnalyticsFilter { LojaId = 3 }, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_ShouldPassLojaIdToAnalytics()
    {
        var analytics = new Mock<IAnaliseCalculoService>();
        analytics.Setup(s => s.CalculateDashboardStatisticsAsync(7, 3))
            .ReturnsAsync(new DashboardSummaryDto());

        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(s => s.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegracaoLoja { Id = 3, UserId = 7, ShopId = "123", FriendlyName = "Loja" });

        var controller = CreateController(analytics.Object, stores.Object);
        var action = await controller.GetDashboardStatsAsync(new DashboardAnalyticsFilter { LojaId = 3 }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(action.Result);
        analytics.Verify(s => s.CalculateDashboardStatisticsAsync(7, 3), Times.Once);
    }

    private static DashboardController CreateController(
        IAnaliseCalculoService analytics,
        IIntegracaoLojaRepository stores,
        string? userId = "7")
    {
        var controller = new DashboardController(analytics, stores);
        var claims = userId is null
            ? Array.Empty<Claim>()
            : [new Claim(ClaimTypes.NameIdentifier, userId)];
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: userId is null ? null : "Test"))
            }
        };
        return controller;
    }
}

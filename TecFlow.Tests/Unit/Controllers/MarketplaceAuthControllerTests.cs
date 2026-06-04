using Microsoft.AspNetCore.Mvc;
using Moq;
using TecFlow.API.Controllers;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Core.Enums;

namespace TecFlow.Tests.Unit.Controllers;

public class MarketplaceAuthControllerTests
{
    [Fact]
    public void GetAuthorizationUrl_ShouldReturnOkWithUrl_WhenServiceGeneratesLink()
    {
        // Arrange
        var auth = new Mock<IMarketplaceAuthService>();
        auth.Setup(s => s.GenerateAuthorizationUrl(
                MarketplaceType.TikTokShop,
                "https://callback",
                "state-1"))
            .Returns("https://auth.tiktok.test/authorize?app_key=1");

        var controller = new MarketplaceAuthController(auth.Object);

        // Act
        var result = controller.GetAuthorizationUrl(
            MarketplaceType.TikTokShop,
            "https://callback",
            "state-1");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnBadRequest_WhenTokenExchangeFails()
    {
        // Arrange
        var auth = new Mock<IMarketplaceAuthService>();
        auth.Setup(s => s.CallbackAndGenerateTokensAsync(
                MarketplaceType.Shopee,
                "bad-code",
                "shop-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketplaceTokenResult
            {
                Success = false,
                Descricao = "Código inválido",
                ShopId = "shop-1",
                MarketplaceType = MarketplaceType.Shopee
            });

        var controller = new MarketplaceAuthController(auth.Object);

        // Act
        var result = await controller.CallbackAsync(
            MarketplaceType.Shopee,
            "bad-code",
            "shop-1",
            CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}

using Microsoft.AspNetCore.Mvc;
using Moq;
using TecFlow.API.Controllers;
using TecFlow.Business.Dto;
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
        var dto = Assert.IsType<MarketplaceAuthorizeUrlResponseDto>(ok.Value);
        Assert.Equal("https://auth.tiktok.test/authorize?app_key=1", dto.AuthorizeUrl);
        Assert.Equal(dto.AuthorizeUrl, dto.AuthorizationUrl);
    }

    [Fact]
    public void GetPlatformAuthorizationUrl_ShouldReturnAuthorizeUrl_ForShopeeSlug()
    {
        var auth = new Mock<IMarketplaceAuthService>();
        auth.Setup(s => s.GenerateAuthorizationUrl(
                MarketplaceType.Shopee,
                "https://callback",
                "ticket-1"))
            .Returns("https://partner.shopee.test/auth?partner_id=1");

        var controller = new MarketplaceAuthController(auth.Object);

        var result = controller.GetPlatformAuthorizationUrl(
            "shopee",
            "https://callback",
            "ticket-1",
            "Loja SP",
            null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MarketplaceAuthorizeUrlResponseDto>(ok.Value);
        Assert.Equal("https://partner.shopee.test/auth?partner_id=1", dto.AuthorizeUrl);
        Assert.Equal("Shopee", dto.Marketplace);
    }

    [Fact]
    public void GetPlatformAuthorizationUrl_ShouldReturnBadRequest_WhenPlatformIsUnknown()
    {
        var controller = new MarketplaceAuthController(new Mock<IMarketplaceAuthService>().Object);

        var result = controller.GetPlatformAuthorizationUrl("amazon", "https://callback");

        Assert.IsType<BadRequestObjectResult>(result.Result);
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

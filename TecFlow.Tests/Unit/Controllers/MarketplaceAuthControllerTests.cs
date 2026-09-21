using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;
using TecFlow.API.Controllers;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;
using TecFlow.Database.Filter;

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

        var controller = new MarketplaceAuthController(
            auth.Object,
            new Mock<IIntegracaoLojaService>().Object,
            NullLogger<MarketplaceAuthController>.Instance);

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

        var controller = new MarketplaceAuthController(
            auth.Object,
            new Mock<IIntegracaoLojaService>().Object,
            NullLogger<MarketplaceAuthController>.Instance);

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
        var controller = new MarketplaceAuthController(
            new Mock<IMarketplaceAuthService>().Object,
            new Mock<IIntegracaoLojaService>().Object,
            NullLogger<MarketplaceAuthController>.Instance);

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
                It.IsAny<CancellationToken>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new MarketplaceTokenResult
            {
                Success = false,
                Descricao = "Código inválido",
                ShopId = "shop-1",
                MarketplaceType = MarketplaceType.Shopee
            });

        var controller = new MarketplaceAuthController(
            auth.Object,
            new Mock<IIntegracaoLojaService>().Object,
            NullLogger<MarketplaceAuthController>.Instance);

        // Act
        var result = await controller.CallbackAsync(
            MarketplaceType.Shopee,
            "bad-code",
            "shop-1",
            CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public void GetPlatformAuthorizationUrl_ShouldReturnOkSandboxUrl_WhenShopeeServiceThrows()
    {
        var auth = new Mock<IMarketplaceAuthService>();
        auth.Setup(s => s.GenerateAuthorizationUrl(
                MarketplaceType.Shopee,
                "https://localhost:7002/integracoes/oauth/callback",
                "ticket-1"))
            .Throws(new InvalidOperationException("Configure Integrations:Shopee:PartnerId e PartnerKey."));

        var controller = new MarketplaceAuthController(
            auth.Object,
            new Mock<IIntegracaoLojaService>().Object,
            NullLogger<MarketplaceAuthController>.Instance);

        var result = controller.GetPlatformAuthorizationUrl(
            "shopee",
            "https://localhost:7002/integracoes/oauth/callback",
            "ticket-1",
            "Loja Homolog",
            null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MarketplaceAuthorizeUrlResponseDto>(ok.Value);
        Assert.Contains(
            ShopeeAuthorizationUrlFactory.AuthPartnerAbsoluteUrl,
            dto.AuthorizeUrl,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("partner_id=", dto.AuthorizeUrl, StringComparison.Ordinal);
        Assert.Equal("Shopee", dto.Marketplace);
    }

    [Fact]
    public async Task ListLojasAsync_ShouldReturnStoresFromService()
    {
        var lojas = new Mock<IIntegracaoLojaService>();
        lojas.Setup(service => service.ListByUserAsync(
                7,
                It.IsAny<IntegracaoLojaFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegracaoLojaResponseDto
            {
                Status = true,
                Descricao = "OK",
                DataList =
                [
                    new MarketplaceAccountDto
                    {
                        Id = 11,
                        UserId = 7,
                        ShopId = "123456",
                        FriendlyName = "Loja Homolog",
                        PlatformType = MarketplaceType.Shopee
                    }
                ]
            });

        var controller = new MarketplaceAuthController(
            new Mock<IMarketplaceAuthService>().Object,
            lojas.Object,
            NullLogger<MarketplaceAuthController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "7")],
                    "Test"))
            }
        };

        var action = await controller.ListLojasAsync(new IntegracaoLojaFilter(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var envelope = Assert.IsType<IntegracaoLojaResponseDto>(ok.Value);
        Assert.True(envelope.Status);
        Assert.Single(envelope.DataList!);
        Assert.Equal("123456", envelope.DataList![0].ShopId);
    }
}

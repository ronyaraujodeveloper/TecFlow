using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
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
        var auth = new Mock<IMarketplaceAuthService>();
        auth.Setup(s => s.GenerateAuthorizationUrl(
                MarketplaceType.TikTokShop,
                "https://callback",
                "state-1"))
            .Returns("https://auth.tiktok.test/authorize?app_key=1");

        var controller = CreateController(auth: auth.Object);
        var result = controller.GetAuthorizationUrl(
            MarketplaceType.TikTokShop,
            "https://callback",
            "state-1");

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

        var controller = CreateController(auth: auth.Object);
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
        var controller = CreateController();
        var result = controller.GetPlatformAuthorizationUrl("aliexpress", "https://callback");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnBadRequest_WhenTokenExchangeFails()
    {
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

        var controller = CreateController(auth: auth.Object);
        var result = await controller.CallbackAsync(
            MarketplaceType.Shopee,
            "bad-code",
            "shop-1",
            CancellationToken.None);

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

        var controller = CreateController(auth: auth.Object);
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

        var controller = CreateController(lojas: lojas.Object, userId: "7");
        var action = await controller.ListLojasAsync(new IntegracaoLojaFilter(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var envelope = Assert.IsType<IntegracaoLojaResponseDto>(ok.Value);
        Assert.True(envelope.Status);
        Assert.Single(envelope.DataList!);
        Assert.Equal("123456", envelope.DataList![0].ShopId);
    }

    [Fact]
    public async Task VincularManualAsync_ShouldUseNameIdentifierClaim_WhenJwtIsValid()
    {
        var lojas = new Mock<IIntegracaoLojaService>();
        lojas.Setup(s => s.LinkAsync(7, It.IsAny<IntegracaoLojaDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegracaoLojaResponseDto { Status = true, Descricao = "OK" });

        var action = await CreateController(lojas: lojas.Object, userId: "7")
            .VincularManualAsync(ValidDto(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(action.Result);
        lojas.Verify(
            s => s.LinkAsync(7, It.IsAny<IntegracaoLojaDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VincularManualAsync_ShouldUseFallbackUserId_WhenClaimIsMissing()
    {
        var lojas = new Mock<IIntegracaoLojaService>();
        lojas.Setup(s => s.LinkAsync(
                HomologMarketplaceAuth.FallbackUserId,
                It.IsAny<IntegracaoLojaDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegracaoLojaResponseDto { Status = true, Descricao = "OK" });

        var action = await CreateController(lojas: lojas.Object, userId: null)
            .VincularManualAsync(ValidDto(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(action.Result);
        lojas.Verify(
            s => s.LinkAsync(HomologMarketplaceAuth.FallbackUserId, It.IsAny<IntegracaoLojaDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VincularManualAsync_ShouldReturnBadRequest_WhenPayloadIsNull()
    {
        var action = await CreateController().VincularManualAsync(null!, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
        var envelope = Assert.IsType<ResponseDto>(badRequest.Value);
        Assert.False(envelope.Status);
        Assert.Equal("Payload de vinculação inválido.", envelope.Descricao);
    }

    [Fact]
    public async Task VincularManualAsync_ShouldReturnJson500_WhenServiceThrows()
    {
        var lojas = new Mock<IIntegracaoLojaService>();
        lojas.Setup(s => s.LinkAsync(7, It.IsAny<IntegracaoLojaDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha inesperada"));

        var action = await CreateController(lojas: lojas.Object).VincularManualAsync(ValidDto(), CancellationToken.None);

        var result = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        var envelope = Assert.IsType<ResponseDto>(result.Value);
        Assert.False(envelope.Status);
        Assert.Equal("Erro do Servidor/SQL: falha inesperada", envelope.Descricao);
    }

    [Fact]
    public async Task VincularManualAsync_ShouldReturnOk_WhenShopeeTrackingIdIsAlphanumeric()
    {
        var lojas = new Mock<IIntegracaoLojaService>();
        lojas.Setup(s => s.LinkAsync(7, It.IsAny<IntegracaoLojaDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegracaoLojaResponseDto
            {
                Status = true,
                Descricao = "Conta Shopee vinculada no modo Universal Link."
            });

        var swapped = new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            AuthorizationCode = "123456",
            ShopId = "code_teste",
            FriendlyName = "Loja Homolog"
        };

        var action = await CreateController(lojas: lojas.Object).VincularManualAsync(swapped, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var envelope = Assert.IsType<MarketplaceAccountResponseDto>(ok.Value);
        Assert.True(envelope.Status);
    }

    [Fact]
    public async Task VincularManualAsync_ShouldReturnOk_WhenLinkSucceeds()
    {
        var lojas = new Mock<IIntegracaoLojaService>();
        lojas.Setup(s => s.LinkAsync(7, It.IsAny<IntegracaoLojaDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegracaoLojaResponseDto
            {
                Status = true,
                Descricao = "Loja vinculada com sucesso."
            });

        var action = await CreateController(lojas: lojas.Object).VincularManualAsync(ValidDto(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var envelope = Assert.IsType<MarketplaceAccountResponseDto>(ok.Value);
        Assert.True(envelope.Status);
        Assert.Equal(HomologMarketplaceAuth.ManualLinkSuccessMessage, envelope.Descricao);
    }

    [Fact]
    public async Task VincularManualAsync_ShouldLogAndReturnBadRequest_WhenModelStateIsInvalid()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError("shopId", "The JSON value could not be converted to String.");

        var action = await controller.VincularManualAsync(ValidDto(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
        var envelope = Assert.IsType<ResponseDto>(badRequest.Value);
        Assert.False(envelope.Status);
        Assert.Contains("shopId", envelope.Descricao, StringComparison.OrdinalIgnoreCase);
    }

    private static MarketplaceAuthController CreateController(
        IMarketplaceAuthService? auth = null,
        IIntegracaoLojaService? lojas = null,
        string? userId = "7")
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns("Homologacao");

        var controller = new MarketplaceAuthController(
            auth ?? new Mock<IMarketplaceAuthService>().Object,
            lojas ?? new Mock<IIntegracaoLojaService>().Object,
            NullLogger<MarketplaceAuthController>.Instance,
            environment.Object);

        var claims = userId is null
            ? Array.Empty<Claim>()
            : [new Claim(ClaimTypes.NameIdentifier, userId)];
        var identity = new ClaimsIdentity(claims, authenticationType: userId is null ? null : "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
        return controller;
    }

    private static IntegracaoLojaDto ValidDto() => new()
    {
        PlatformType = MarketplaceType.Shopee,
        AuthorizationCode = "code_teste",
        ShopId = "123456",
        FriendlyName = "Loja Homolog"
    };
}

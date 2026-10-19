using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ofertio.API.Controllers;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Enums;
using Ofertio.Infrastructure.Services.Integrations.Auth;

namespace Ofertio.Tests.Unit.Controllers;

public class MercadoLivreControllerTests
{
    private static readonly string Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private const string Callback = "https://localhost:7002/integracoes/oauth/callback";

    [Fact]
    public async Task Callback_ShouldSaveTokensAndRedirectToOriginAccountsPage()
    {
        var protector = new OAuthStateProtector(Key);
        var sealedState = protector.Protect(MarketplaceOAuthStateRules.Create(7, Guid.NewGuid(), MarketplaceType.MercadoLivre, "Loja ML", Callback));
        var lojas = new Mock<IIntegracaoLojaService>();
        lojas.Setup(s => s.LinkAsync(7, It.IsAny<IntegracaoLojaDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegracaoLojaResponseDto { Status = true, Descricao = "ok" });
        var controller = CreateController(lojas.Object, protector);

        var result = await controller.CallbackAsync("TG-code", sealedState, null, null, CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://localhost:7002/marketplace-accounts?status=success", redirect.Url);
        lojas.Verify(s => s.LinkAsync(
            7,
            It.Is<IntegracaoLojaDto>(dto => dto.AuthorizationCode == "TG-code" && dto.RedirectUri == Callback),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Callback_ShouldReturnJsonBadRequestForForgedState()
    {
        var lojas = new Mock<IIntegracaoLojaService>();
        var controller = CreateController(lojas.Object, new OAuthStateProtector(Key));

        var result = await controller.CallbackAsync("TG-code", MarketplaceOAuthStateRules.Prefix + "forjado", null, null, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, status.StatusCode);
        Assert.False(Assert.IsType<MarketplaceOAuthCallbackResultDto>(status.Value).Success);
        lojas.Verify(s => s.LinkAsync(It.IsAny<int>(), It.IsAny<IntegracaoLojaDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Callback_ShouldRedirectWithErrorWhenUserDeniesAuthorization()
    {
        var protector = new OAuthStateProtector(Key);
        var sealedState = protector.Protect(MarketplaceOAuthStateRules.Create(7, null, MarketplaceType.MercadoLivre, "Loja ML", Callback));
        var lojas = new Mock<IIntegracaoLojaService>();
        var controller = CreateController(lojas.Object, protector);

        var result = await controller.CallbackAsync(null, sealedState, "access_denied", null, CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.StartsWith("https://localhost:7002/marketplace-accounts?status=error", redirect.Url, StringComparison.Ordinal);
        lojas.Verify(s => s.LinkAsync(It.IsAny<int>(), It.IsAny<IntegracaoLojaDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("{\"_id\":\"abc\",\"resource\":\"/orders/2000000000\",\"user_id\":123456,\"topic\":\"orders_v2\",\"application_id\":\"987\",\"attempts\":1,\"sent\":\"2026-10-19T20:00:00.000Z\",\"received\":\"2026-10-19T20:00:00.000Z\"}")]
    [InlineData("")]
    [InlineData("{json quebrado")]
    public async Task Notifications_ShouldAlwaysReturnOk(string body)
    {
        var controller = CreateController(new Mock<IIntegracaoLojaService>().Object, new OAuthStateProtector(Key));
        controller.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

        var result = await controller.NotificationsAsync(CancellationToken.None);

        Assert.IsType<OkResult>(result);
    }

    private static MercadoLivreController CreateController(IIntegracaoLojaService lojas, IOAuthStateProtector protector)
    {
        var controller = new MercadoLivreController(
            lojas,
            NullLogger<MercadoLivreController>.Instance,
            protector,
            new MemoryCache(new MemoryCacheOptions()));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }
}

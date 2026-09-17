using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TecFlow.API.Controllers;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;

namespace TecFlow.Tests.Unit.Controllers;

public class IntegracoesControllerTests
{
    [Fact]
    public async Task VincularManualAsync_ShouldReturnBadRequest_WhenPayloadIsNull()
    {
        var controller = CreateController(new Mock<IIntegracaoLojaService>().Object);

        var action = await controller.VincularManualAsync(null!, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
        Assert.Equal(400, badRequest.StatusCode);
        var envelope = Assert.IsType<IntegracaoLojaResponseDto>(badRequest.Value);
        Assert.False(envelope.Status);
        Assert.Equal("Payload de vinculação inválido.", envelope.Descricao);
    }

    [Fact]
    public async Task VincularManualAsync_ShouldReturnBadRequest_WhenServiceThrows()
    {
        var service = new Mock<IIntegracaoLojaService>();
        service.Setup(s => s.LinkAsync(
                7,
                It.IsAny<IntegracaoLojaDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha inesperada"));

        var controller = CreateController(service.Object);
        var action = await controller.VincularManualAsync(ValidDto(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
        var envelope = Assert.IsType<IntegracaoLojaResponseDto>(badRequest.Value);
        Assert.False(envelope.Status);
        Assert.Equal("Não foi possível vincular a loja. Tente novamente.", envelope.Descricao);
    }

    [Fact]
    public async Task VincularManualAsync_ShouldReturnBadRequestEnvelope_WhenShopIdAndCodeAreSwapped()
    {
        var service = new Mock<IIntegracaoLojaService>();
        service.Setup(s => s.LinkAsync(7, It.IsAny<IntegracaoLojaDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegracaoLojaResponseDto
            {
                Status = false,
                Descricao = "Shop ID da Shopee deve ser um número inteiro (ex.: 123456)."
            });

        var controller = CreateController(service.Object);
        var swapped = new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            AuthorizationCode = "123456",
            ShopId = "code_teste",
            FriendlyName = "Loja Homolog"
        };

        var action = await controller.VincularManualAsync(swapped, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
        var envelope = Assert.IsType<IntegracaoLojaResponseDto>(badRequest.Value);
        Assert.False(envelope.Status);
        Assert.Contains("número inteiro", envelope.Descricao, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VincularManualAsync_ShouldReturnOk_WhenLinkSucceeds()
    {
        var service = new Mock<IIntegracaoLojaService>();
        service.Setup(s => s.LinkAsync(7, It.IsAny<IntegracaoLojaDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegracaoLojaResponseDto
            {
                Status = true,
                Descricao = "Loja vinculada com sucesso."
            });

        var controller = CreateController(service.Object);
        var action = await controller.VincularManualAsync(ValidDto(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var envelope = Assert.IsType<IntegracaoLojaResponseDto>(ok.Value);
        Assert.True(envelope.Status);
    }

    [Fact]
    public async Task ListAsync_ShouldReturn401_WhenUserIsMissing()
    {
        var controller = CreateController(new Mock<IIntegracaoLojaService>().Object, userId: null);
        var action = await controller.ListAsync(new TecFlow.Database.Filter.IntegracaoLojaFilter(), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(action.Result);
        Assert.Equal(401, unauthorized.StatusCode);
        Assert.False(Assert.IsType<IntegracaoLojaResponseDto>(unauthorized.Value).Status);
    }

    private static IntegracoesController CreateController(IIntegracaoLojaService service, string? userId = "7")
    {
        var controller = new IntegracoesController(service);
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

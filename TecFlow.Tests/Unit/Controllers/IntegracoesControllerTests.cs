using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.API.Controllers;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Tests.Unit.Controllers;

public class IntegracoesControllerTests
{
    [Fact]
    public async Task ListAsync_ShouldReturn401_WhenUserIsMissing()
    {
        var controller = CreateController(new Mock<IIntegracaoLojaService>().Object, userId: null);
        var action = await controller.ListAsync(new TecFlow.Database.Filter.IntegracaoLojaFilter(), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(action.Result);
        Assert.Equal(401, unauthorized.StatusCode);
        Assert.False(Assert.IsType<IntegracaoLojaResponseDto>(unauthorized.Value).Status);
    }

    [Fact]
    public async Task LinkAsync_ShouldReturnJson500_WhenServiceThrows()
    {
        var service = new Mock<IIntegracaoLojaService>();
        service.Setup(s => s.LinkAsync(
                7,
                It.IsAny<IntegracaoLojaDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha inesperada"));

        var controller = CreateController(service.Object);
        var action = await controller.LinkAsync(new IntegracaoLojaDto
        {
            AuthorizationCode = "code_teste",
            ShopId = "123456",
            FriendlyName = "Loja Homolog"
        }, CancellationToken.None);

        var result = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        var envelope = Assert.IsType<ResponseDto>(result.Value);
        Assert.False(envelope.Status);
        Assert.Equal("Erro do Servidor/SQL: falha inesperada", envelope.Descricao);
    }

    private static IntegracoesController CreateController(IIntegracaoLojaService service, string? userId = "7")
    {
        var controller = new IntegracoesController(
            service,
            NullLogger<IntegracoesController>.Instance,
            CreateEnvironment("Homologacao"));
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

    private static IHostEnvironment CreateEnvironment(string name)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns(name);
        return environment.Object;
    }
}

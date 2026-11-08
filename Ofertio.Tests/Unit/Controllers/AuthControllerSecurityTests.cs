using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ofertio.API.Controllers;
using Ofertio.Business.Dto;
using Ofertio.Business.Dto.Auth;
using Ofertio.Business.Interfaces.Services;

namespace Ofertio.Tests.Unit.Controllers;

public class AuthControllerSecurityTests
{
    [Fact]
    public async Task ChangePasswordAsync_ShouldReturn401_WhenUserIsMissing()
    {
        var controller = CreateController(new Mock<IPlatformAuthService>().Object, userId: null);

        var action = await controller.ChangePasswordAsync(
            new ChangePasswordDto { CurrentPassword = "a", NewPassword = "b" },
            CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(action.Result);
        Assert.Equal(401, unauthorized.StatusCode);
        var envelope = Assert.IsType<AuthProviderResponseDto>(unauthorized.Value);
        Assert.False(envelope.Status);
        Assert.Equal("Usuário não autenticado.", envelope.Descricao);
    }

    [Fact]
    public async Task GetProviderStatusAsync_ShouldReturn500Envelope_WhenServiceThrows()
    {
        var auth = new Mock<IPlatformAuthService>();
        auth.Setup(s => s.GetProviderStatusAsync(7, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var controller = CreateController(auth.Object);
        var action = await controller.GetProviderStatusAsync(CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(500, status.StatusCode);
        var envelope = Assert.IsType<AuthProviderResponseDto>(status.Value);
        Assert.False(envelope.Status);
        Assert.Contains("segurança", envelope.Descricao, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LinkProviderAsync_ShouldRoundTripJsonAndReturnOk()
    {
        const string json = """{"provider":"Google","accessToken":"tok","idToken":"id.jwt"}""";
        var dto = System.Text.Json.JsonSerializer.Deserialize<LinkProviderDto>(
            json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var auth = new Mock<IPlatformAuthService>();
        auth.Setup(s => s.LinkProviderAsync(7, It.IsAny<LinkProviderDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthProviderResponseDto { Status = true, Descricao = "Vinculado." });

        var controller = CreateController(auth.Object);
        var action = await controller.LinkProviderAsync(dto!, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Equal("Google", dto!.Provider);
        Assert.True(Assert.IsType<AuthProviderResponseDto>(ok.Value).Status);
    }

    [Fact]
    public async Task TikTokLogin_ShouldReturn401_WhenCredentialsAreInvalid()
    {
        var auth = new Mock<IPlatformAuthService>();
        auth.Setup(s => s.LoginAsync("TikTok", It.IsAny<PlatformAuthDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null, "Credenciais inválidas.", "INVALID_CREDENTIALS"));

        var controller = CreateController(auth.Object, userId: null);
        var result = await controller.TikTokLogin(
            new PlatformAuthDto
            {
                Provider = "EmailPassword",
                Email = "demo@tecso.local",
                Password = "wrong"
            },
            CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public async Task UnlinkProviderAsync_ShouldReturn400_WhenBusinessRuleFails()
    {
        var auth = new Mock<IPlatformAuthService>();
        auth.Setup(s => s.UnlinkProviderAsync(7, "Google", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthProviderResponseDto
            {
                Status = false,
                Descricao = "É necessário manter ao menos um método de acesso."
            });

        var controller = CreateController(auth.Object);
        var action = await controller.UnlinkProviderAsync("Google", CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(action.Result);
        Assert.False(Assert.IsType<AuthProviderResponseDto>(bad.Value).Status);
    }

    [Fact]
    public async Task RegisterAsync_ShouldKeepTheCloudflareIpInsteadOfTheProxy()
    {
        var auth = new Mock<IPlatformAuthService>();
        auth.Setup(service => service.RegisterAsync(It.IsAny<UserDto>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResponseDto { Status = true, Descricao = "Conta criada com sucesso." });

        var controller = CreateController(auth.Object, userId: null);
        controller.HttpContext.Request.Headers["CF-Connecting-IP"] = "203.0.113.10";
        controller.HttpContext.Request.Headers["X-Forwarded-For"] = "198.51.100.4, 10.0.0.2";
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.8");

        var action = await controller.RegisterAsync(
            new UserDto { Email = "ana@ofertio.local", PasswordHash = "Senha!123", AcceptedTerms = true },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(action.Result);
        auth.Verify(service => service.RegisterAsync(
            It.Is<UserDto>(dto => dto.AcceptedTerms),
            "203.0.113.10",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AuthController CreateController(IPlatformAuthService service, string? userId = "7")
    {
        var controller = new AuthController(service, NullLogger<AuthController>.Instance);
        var claims = userId is null
            ? Array.Empty<Claim>()
            : [new Claim(ClaimTypes.NameIdentifier, userId)];
        var identity = new ClaimsIdentity(claims, authenticationType: userId is null ? null : "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }
}

using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ofertio.API.Controllers;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Enums;
using Ofertio.Core.Security;
using Ofertio.Infrastructure.Services.Integrations.Auth;

namespace Ofertio.Tests.Unit.Integrations;

public class MarketplaceOAuthStateTests
{
    private static readonly string Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private static readonly Guid TenantId = Guid.Parse("8f0c2b0e-6a1d-4c1e-9d2f-1b2c3d4e5f60");
    private const string Callback = "https://localhost:7002/integracoes/oauth/callback";

    [Fact]
    public void Protector_ShouldRoundTripTenantAndUser()
    {
        var protector = new OAuthStateProtector(Key);
        var sealedState = protector.Protect(MarketplaceOAuthStateRules.Create(7, TenantId, MarketplaceType.MercadoLivre, "Loja ML", Callback));

        Assert.StartsWith(MarketplaceOAuthStateRules.Prefix, sealedState, StringComparison.Ordinal);
        Assert.DoesNotContain("Loja", sealedState, StringComparison.Ordinal);
        Assert.True(protector.TryUnprotect(sealedState, out var opened));
        Assert.Equal(7, opened!.UserId);
        Assert.Equal(TenantId, opened.TenantId);
        Assert.Equal("Loja ML", opened.FriendlyName);
        Assert.Equal(Callback, opened.CallbackUri);
    }

    [Fact]
    public void Protector_ShouldRejectTamperedForeignExpiredOrPlainStates()
    {
        var protector = new OAuthStateProtector(Key);
        var sealedState = protector.Protect(MarketplaceOAuthStateRules.Create(7, TenantId, MarketplaceType.MercadoLivre, "Loja", Callback));
        var lastChar = sealedState[^2] == 'A' ? 'B' : 'A';
        var tampered = sealedState[..^2] + lastChar + sealedState[^1];
        var foreign = new OAuthStateProtector(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        var expired = protector.Protect(MarketplaceOAuthStateRules.Create(
            7, TenantId, MarketplaceType.MercadoLivre, "Loja", Callback, DateTimeOffset.UtcNow.AddHours(-1)));

        Assert.False(protector.TryUnprotect(tampered, out _));
        Assert.False(foreign.TryUnprotect(sealedState, out _));
        Assert.False(protector.TryUnprotect(expired, out _));
        Assert.False(protector.TryUnprotect("ticket-123", out _));
        Assert.False(protector.TryUnprotect(MarketplaceOAuthStateRules.Prefix + "@@@", out _));
    }

    [Theory]
    [InlineData(Callback, true, null, "https://localhost:7002/marketplace-accounts?status=success")]
    [InlineData("https://camcorder-bonding-sloppily.ngrok-free.dev/integracoes/oauth/callback", true, null, "https://camcorder-bonding-sloppily.ngrok-free.dev/marketplace-accounts?status=success")]
    [InlineData(null, true, null, "/marketplace-accounts?status=success")]
    [InlineData(Callback, false, "Falhou", "https://localhost:7002/marketplace-accounts?status=error&message=Falhou")]
    public void BuildReturnUrl_ShouldPointToOriginAccountsPage(string? callback, bool success, string? message, string expected)
    {
        Assert.Equal(expected, MarketplaceOAuthStateRules.BuildReturnUrl(callback, success, message));
    }

    [Fact]
    public async Task AuthorizeUrl_ShouldSealUserAndTenantInMercadoLivreState()
    {
        string? sentState = null;
        var auth = new Mock<IMarketplaceAuthService>();
        auth.Setup(s => s.GenerateAuthorizationUrl(MarketplaceType.MercadoLivre, Callback, It.IsAny<string?>()))
            .Callback<MarketplaceType, string, string?>((_, _, state) => sentState = state)
            .Returns("https://auth.mercadolivre.com.br/authorization?state=x");
        var protector = new OAuthStateProtector(Key);
        var controller = CreateController(auth.Object, new Mock<IIntegracaoLojaService>().Object, protector, authenticated: true);

        var result = await controller.GetPlatformAuthorizationUrl("mercadolivre", Callback, "ticket-1", "Loja ML");

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(protector.TryUnprotect(sentState, out var opened));
        Assert.Equal(7, opened!.UserId);
        Assert.Equal(TenantId, opened.TenantId);
        Assert.Equal("Loja ML", opened.FriendlyName);
    }

    [Fact]
    public async Task AuthorizeUrl_ShouldKeepPlainStateForOtherPlatforms()
    {
        var auth = new Mock<IMarketplaceAuthService>();
        auth.Setup(s => s.GenerateAuthorizationUrl(MarketplaceType.Shopee, Callback, "ticket-1"))
            .Returns("https://partner.shopee.test/auth");
        var controller = CreateController(auth.Object, new Mock<IIntegracaoLojaService>().Object, new OAuthStateProtector(Key), authenticated: true);

        var result = await controller.GetPlatformAuthorizationUrl("shopee", Callback, "ticket-1", "Loja SP");

        Assert.IsType<OkObjectResult>(result.Result);
        auth.Verify(s => s.GenerateAuthorizationUrl(MarketplaceType.Shopee, Callback, "ticket-1"), Times.Once);
    }

    [Fact]
    public async Task AnonymousCallback_ShouldSaveTokensForSealedUserAndReturnToOrigin()
    {
        var protector = new OAuthStateProtector(Key);
        var sealedState = protector.Protect(MarketplaceOAuthStateRules.Create(7, TenantId, MarketplaceType.MercadoLivre, "Loja ML", Callback));
        IntegracaoLojaDto? linked = null;
        string? tenantDuringLink = null;
        var lojas = new Mock<IIntegracaoLojaService>();
        var controller = CreateController(new Mock<IMarketplaceAuthService>().Object, lojas.Object, protector, authenticated: false);
        lojas.Setup(s => s.LinkAsync(7, It.IsAny<IntegracaoLojaDto>(), It.IsAny<CancellationToken>()))
            .Callback<int, IntegracaoLojaDto, CancellationToken>((_, dto, _) =>
            {
                linked = dto;
                tenantDuringLink = controller.HttpContext.User.FindFirst(OfertioClaimTypes.TenantId)?.Value;
            })
            .ReturnsAsync(new IntegracaoLojaResponseDto { Status = true, Descricao = "ok" });

        var result = await controller.CompleteMercadoLivreCallbackAsync(
            new MarketplaceOAuthCallbackRequestDto { Code = "TG-code", State = sealedState },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<MarketplaceOAuthCallbackResultDto>(ok.Value);
        Assert.True(body.Success);
        Assert.Equal("https://localhost:7002/marketplace-accounts?status=success", body.ReturnUrl);
        Assert.Equal(MarketplaceType.MercadoLivre, linked!.PlatformType);
        Assert.Equal("TG-code", linked.AuthorizationCode);
        Assert.Equal("Loja ML", linked.FriendlyName);
        Assert.Equal(Callback, linked.RedirectUri);
        Assert.Equal(TenantId.ToString(), tenantDuringLink);
    }

    [Fact]
    public async Task AnonymousCallback_ShouldRejectReplayedState()
    {
        var protector = new OAuthStateProtector(Key);
        var sealedState = protector.Protect(MarketplaceOAuthStateRules.Create(7, TenantId, MarketplaceType.MercadoLivre, "Loja ML", Callback));
        var lojas = new Mock<IIntegracaoLojaService>();
        lojas.Setup(s => s.LinkAsync(7, It.IsAny<IntegracaoLojaDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegracaoLojaResponseDto { Status = true });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var request = new MarketplaceOAuthCallbackRequestDto { Code = "TG-code", State = sealedState };

        await CreateController(new Mock<IMarketplaceAuthService>().Object, lojas.Object, protector, false, cache)
            .CompleteMercadoLivreCallbackAsync(request, CancellationToken.None);
        var second = await CreateController(new Mock<IMarketplaceAuthService>().Object, lojas.Object, protector, false, cache)
            .CompleteMercadoLivreCallbackAsync(request, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(second.Result);
        Assert.Equal(MarketplaceOAuthStateRules.ReplayedStateMessage, Assert.IsType<MarketplaceOAuthCallbackResultDto>(bad.Value).Message);
        lojas.Verify(s => s.LinkAsync(It.IsAny<int>(), It.IsAny<IntegracaoLojaDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnonymousCallback_ShouldRejectForgedState()
    {
        var lojas = new Mock<IIntegracaoLojaService>();
        var controller = CreateController(new Mock<IMarketplaceAuthService>().Object, lojas.Object, new OAuthStateProtector(Key), authenticated: false);

        var result = await controller.CompleteMercadoLivreCallbackAsync(
            new MarketplaceOAuthCallbackRequestDto { Code = "TG-code", State = MarketplaceOAuthStateRules.Prefix + "forjado" },
            CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.StartsWith("/marketplace-accounts?status=error", Assert.IsType<MarketplaceOAuthCallbackResultDto>(bad.Value).ReturnUrl, StringComparison.Ordinal);
        lojas.Verify(s => s.LinkAsync(It.IsAny<int>(), It.IsAny<IntegracaoLojaDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static MarketplaceAuthController CreateController(
        IMarketplaceAuthService auth,
        IIntegracaoLojaService lojas,
        IOAuthStateProtector protector,
        bool authenticated,
        IMemoryCache? cache = null)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns("Homologacao");
        var controller = new MarketplaceAuthController(
            auth,
            lojas,
            NullLogger<MarketplaceAuthController>.Instance,
            environment.Object,
            protector,
            cache ?? new MemoryCache(new MemoryCacheOptions()));
        var identity = authenticated
            ? new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "7"), new Claim(OfertioClaimTypes.TenantId, TenantId.ToString())],
                "Test")
            : new ClaimsIdentity();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Integrations.Shopee;
using Ofertio.Business.Interfaces.Repositories;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;
using Ofertio.Database.MultiTenancy;
using Ofertio.Infrastructure.Services.Integrations.Auth;
using Ofertio.Tests.Helpers;
using Ofertio.Util.Security;

namespace Ofertio.Tests.Unit.Integrations;

public class MarketplaceAuthServiceTests
{
    private readonly Mock<IMarketplaceTokenRepository> _tokenRepository = new();
    private readonly Mock<IMarketplaceAccountRepository> _accountRepository = new();
    private readonly Mock<ICurrentTenantService> _currentTenant = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly MarketplaceSignatureService _signatureService = new();

    private readonly Mock<IHostEnvironment> _hostEnvironment = new();
    private readonly Mock<ITenantProvisioningService> _tenants = new();

    private readonly AppDbContext _db = CreateDbContext();

    public MarketplaceAuthServiceTests()
    {
        var tenantId = Guid.NewGuid();
        _currentTenant.Setup(t => t.TenantId).Returns(tenantId);
        _hostEnvironment.SetupGet(environment => environment.EnvironmentName).Returns("Homologacao");
        _tenants.Setup(service => service.EnsurePersistedTenantAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = tenantId, Name = "Tenant Principal", IsActive = true });
        _tenants.Setup(service => service.EnsureTenantForUserAsync(It.IsAny<UserAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = tenantId, Name = "Tenant Principal", IsActive = true });
    }

    private MarketplaceAuthService CreateService() =>
        new(
            _tokenRepository.Object,
            _accountRepository.Object,
            _db,
            _currentTenant.Object,
            _signatureService,
            _httpClientFactory.Object,
            MarketplaceTestOptionsFactory.TikTokOptions(),
            MarketplaceTestOptionsFactory.ShopeeOptions(),
            NullLogger<MarketplaceAuthService>.Instance,
            _hostEnvironment.Object,
            _tenants.Object);

    private static AppDbContext CreateDbContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(item => item.Encrypt(It.IsAny<string>())).Returns<string>(value => value);
        encryption.Setup(item => item.Decrypt(It.IsAny<string>())).Returns<string>(value => value);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, encryption.Object, new NullCurrentTenantService());
    }

    [Fact]
    public void GenerateAuthorizationUrl_ShouldContainAppKey_WhenMarketplaceIsTikTokShop()
    {
        // Arrange
        var service = CreateService();

        // Act
        var url = service.GenerateAuthorizationUrl(
            MarketplaceType.TikTokShop,
            "https://app.ofertio.test/callback",
            "state-123");

        // Assert
        Assert.Contains(MarketplaceTestOptionsFactory.TikTokAppKey, url);
        Assert.Contains("redirect_uri=", url);
        Assert.Contains("state=state-123", url);
    }

    [Fact]
    public void GenerateAuthorizationUrl_ShouldContainPartnerId_WhenMarketplaceIsShopee()
    {
        // Arrange
        var service = CreateService();

        // Act
        var url = service.GenerateAuthorizationUrl(
            MarketplaceType.Shopee,
            "https://app.ofertio.test/callback",
            "state-shopee");

        // Assert
        Assert.Contains($"partner_id={MarketplaceTestOptionsFactory.ShopeePartnerId}", url);
        Assert.Contains("sign=", url);
        Assert.Contains("state-shopee", url);
    }

    [Fact]
    public void GenerateAuthorizationUrl_ShouldReturnSandboxShopeeUrl_WhenPartnerCredentialsAreEmpty()
    {
        var service = new MarketplaceAuthService(
            _tokenRepository.Object,
            _accountRepository.Object,
            CreateDbContext(),
            _currentTenant.Object,
            _signatureService,
            _httpClientFactory.Object,
            MarketplaceTestOptionsFactory.TikTokOptions(),
            Options.Create(new ShopeeIntegrationOptions
            {
                PartnerId = string.Empty,
                PartnerKey = string.Empty,
                ApiBaseUrl = string.Empty,
                AuthPartnerPath = string.Empty
            }),
            NullLogger<MarketplaceAuthService>.Instance,
            _hostEnvironment.Object,
            _tenants.Object);

        var url = service.GenerateAuthorizationUrl(
            MarketplaceType.Shopee,
            "https://localhost:7002/integracoes/oauth/callback",
            "ticket-homolog");

        Assert.Contains(
            ShopeeAuthorizationUrlFactory.AuthPartnerAbsoluteUrl,
            url,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"partner_id={ShopeeAuthorizationUrlFactory.SandboxPartnerId}", url);
        Assert.Contains("sign=", url);
        Assert.Contains("shop/auth_partner", url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ticket-homolog", url);
    }

    [Fact]
    public void GenerateAuthorizationUrl_ShouldThrow_WhenRedirectUriIsEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act / Assert
        Assert.Throws<ArgumentException>(() =>
            service.GenerateAuthorizationUrl(MarketplaceType.Shopee, ""));
    }

    [Fact]
    public async Task CallbackAndGenerateTokensAsync_ShouldPersistTestTokens_WhenHomologCodeIsStub()
    {
        var service = CreateService();
        _tokenRepository
            .Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.CallbackAndGenerateTokensAsync(
            MarketplaceType.Shopee,
            HomologMarketplaceAuth.StubAuthorizationCode,
            "123456",
            userId: "7");

        Assert.True(result.Success);
        Assert.Equal("123456", result.ShopId);
        Assert.Equal(HomologMarketplaceAuth.ManualLinkSuccessMessage, result.Descricao);
        var savedAccount = Assert.Single(_db.MarketplaceAccounts);
        Assert.Equal("7", savedAccount.UserId);
        Assert.Equal(HomologMarketplaceAuth.StubAccessToken, savedAccount.AccessToken);
        _httpClientFactory.Verify(factory => factory.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CallbackAndGenerateTokensAsync_ShouldSkipRemoteOAuth_WhenCodeHasHomologPrefixInProduction()
    {
        _hostEnvironment.SetupGet(environment => environment.EnvironmentName).Returns("Production");
        var service = CreateService();
        _accountRepository
            .Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()))
            .Returns(Task.CompletedTask);
        _tokenRepository
            .Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.CallbackAndGenerateTokensAsync(
            MarketplaceType.Shopee,
            "code_homolog_local",
            "123456",
            userId: "7");

        Assert.True(result.Success);
        Assert.Equal(HomologMarketplaceAuth.ManualLinkSuccessMessage, result.Descricao);
        _httpClientFactory.Verify(factory => factory.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CallbackAndGenerateTokensAsync_ShouldSkipRemoteOAuth_WhenEnvironmentIsHomologacao()
    {
        var service = CreateService();
        _accountRepository
            .Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()))
            .Returns(Task.CompletedTask);
        _tokenRepository
            .Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.CallbackAndGenerateTokensAsync(
            MarketplaceType.Shopee,
            "oauth-real-looking-code",
            "123456",
            userId: "7");

        Assert.True(result.Success);
        _httpClientFactory.Verify(factory => factory.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetValidTokenAsync_ShouldReturnExistingToken_WhenNotExpired()
    {
        // Arrange
        const string shopId = "shop-1";
        var service = CreateService();
        _tokenRepository
            .Setup(r => r.GetByShopAndMarketplaceAsync(shopId, MarketplaceType.Shopee))
            .ReturnsAsync(new MarketplaceToken
            {
                ShopId = shopId,
                MarketplaceType = MarketplaceType.Shopee,
                AccessToken = "valid-token",
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            });

        // Act
        var token = await service.GetValidTokenAsync(shopId, MarketplaceType.Shopee);

        // Assert
        Assert.Equal("valid-token", token);
        _tokenRepository.Verify(r => r.UpsertAsync(It.IsAny<MarketplaceToken>()), Times.Never);
    }

    [Fact]
    public async Task GetValidTokenAsync_ShouldThrow_WhenTokenNotFound()
    {
        // Arrange
        var service = CreateService();
        _tokenRepository
            .Setup(r => r.GetByShopAndMarketplaceAsync("missing", MarketplaceType.TikTokShop))
            .ReturnsAsync((MarketplaceToken?)null);

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetValidTokenAsync("missing", MarketplaceType.TikTokShop));
    }

    [Fact]
    public async Task GetValidTokenAsync_ShouldRefreshAndUpsert_WhenTokenExpired()
    {
        // Arrange
        const string shopId = "shop-refresh";
        var service = CreateService();
        var stored = new MarketplaceToken
        {
            ShopId = shopId,
            MarketplaceType = MarketplaceType.TikTokShop,
            AccessToken = "old-token",
            RefreshToken = "refresh-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10)
        };

        _tokenRepository
            .Setup(r => r.GetByShopAndMarketplaceAsync(shopId, MarketplaceType.TikTokShop))
            .ReturnsAsync(stored);

        var oauthJson = """
            {
              "access_token": "new-token",
              "refresh_token": "new-refresh",
              "expire_in": 7200
            }
            """;

        var handler = StubHttpMessageHandler.WithJsonResponse(oauthJson);
        var client = new HttpClient(handler);
        _httpClientFactory
            .Setup(f => f.CreateClient("Ofertio.MarketplaceOAuth"))
            .Returns(client);

        // Act
        var token = await service.GetValidTokenAsync(shopId, MarketplaceType.TikTokShop);

        // Assert
        Assert.Equal("new-token", token);
        _tokenRepository.Verify(r => r.UpsertAsync(It.Is<MarketplaceToken>(t => t.AccessToken == "new-token")), Times.Once);
    }

    private MarketplaceAuthService CreateMercadoLivreService(MercadoLivreIntegrationOptions options) =>
        new(
            _tokenRepository.Object,
            _accountRepository.Object,
            _db,
            _currentTenant.Object,
            _signatureService,
            _httpClientFactory.Object,
            MarketplaceTestOptionsFactory.TikTokOptions(),
            MarketplaceTestOptionsFactory.ShopeeOptions(),
            NullLogger<MarketplaceAuthService>.Instance,
            _hostEnvironment.Object,
            _tenants.Object,
            Options.Create(options));

    [Fact]
    public void GenerateAuthorizationUrl_ShouldUseConfiguredRedirect_WhenMarketplaceIsMercadoLivre()
    {
        var service = CreateMercadoLivreService(new MercadoLivreIntegrationOptions
        {
            AppId = "123456",
            SecretKey = "secret",
            RedirectUri = "https://tunnel.test/integracoes/oauth/callback"
        });

        var url = service.GenerateAuthorizationUrl(
            MarketplaceType.MercadoLivre,
            "http://localhost:5000/integracoes/oauth/callback",
            "ticket-ml");

        Assert.StartsWith("https://auth.mercadolivre.com.br/authorization?", url, StringComparison.Ordinal);
        Assert.Contains("response_type=code", url);
        Assert.Contains("client_id=123456", url);
        Assert.Contains("redirect_uri=https%3A%2F%2Ftunnel.test%2Fintegracoes%2Foauth%2Fcallback", url);
        Assert.Contains("state=ticket-ml", url);
    }

    [Fact]
    public async Task MercadoLivreOAuth_ShouldUsePublicApiCallback_InAuthorizeUrlAndTokenExchange()
    {
        const string publicCallback = "https://dev.ofertio.com.br/api/mercadolivre/callback";
        string? body = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""{"access_token":"APP_USR-1","expires_in":21600,"refresh_token":"TG-r"}""")
            };
        });
        _httpClientFactory
            .Setup(f => f.CreateClient("Ofertio.MarketplaceOAuth"))
            .Returns(new HttpClient(handler));
        var service = CreateMercadoLivreService(new MercadoLivreIntegrationOptions
        {
            AppId = "123456",
            SecretKey = "secret",
            RedirectUri = publicCallback
        });

        var url = service.GenerateAuthorizationUrl(
            MarketplaceType.MercadoLivre,
            "https://localhost:7002/integracoes/oauth/callback",
            "ticket-ml");
        await service.ExchangeMercadoLivreCodeAsync("TG-code", "https://localhost:7002/integracoes/oauth/callback");

        var expected = "redirect_uri=" + Uri.EscapeDataString(publicCallback);
        Assert.Contains(expected + "&", url);
        Assert.Contains(expected, body);
    }

    [Theory]
    [InlineData("https://camcorder-bonding-sloppily.ngrok-free.dev:7002/integracoes/oauth/callback")]
    [InlineData("http://camcorder-bonding-sloppily.ngrok-free.dev:5000/integracoes/oauth/callback")]
    [InlineData("https://camcorder-bonding-sloppily.ngrok-free.dev:7002")]
    public void GenerateAuthorizationUrl_ShouldStripLocalPort_WhenRedirectIsNgrok(string configured)
    {
        var service = CreateMercadoLivreService(new MercadoLivreIntegrationOptions
        {
            AppId = "123456",
            SecretKey = "secret",
            RedirectUri = configured
        });

        var url = service.GenerateAuthorizationUrl(MarketplaceType.MercadoLivre, "https://localhost:7002/integracoes/oauth/callback", "s");

        Assert.Contains(
            "redirect_uri=" + Uri.EscapeDataString("https://camcorder-bonding-sloppily.ngrok-free.dev/integracoes/oauth/callback") + "&",
            url);
    }

    [Fact]
    public async Task ExchangeMercadoLivreCodeAsync_ShouldSendSameCleanNgrokRedirect()
    {
        string? body = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""{"access_token":"APP_USR-1","expires_in":21600,"refresh_token":"TG-r"}""")
            };
        });
        _httpClientFactory
            .Setup(f => f.CreateClient("Ofertio.MarketplaceOAuth"))
            .Returns(new HttpClient(handler));
        var service = CreateMercadoLivreService(new MercadoLivreIntegrationOptions { AppId = "123456", SecretKey = "secret" });

        await service.ExchangeMercadoLivreCodeAsync("TG-code", "https://camcorder-bonding-sloppily.ngrok-free.dev:7002/integracoes/oauth/callback");

        Assert.Contains(
            "redirect_uri=https%3A%2F%2Fcamcorder-bonding-sloppily.ngrok-free.dev%2Fintegracoes%2Foauth%2Fcallback",
            body);
    }

    [Fact]
    public void GenerateAuthorizationUrl_ShouldExplainMissingAppId_WhenMercadoLivreIsNotConfigured()
    {
        var service = CreateMercadoLivreService(new MercadoLivreIntegrationOptions());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            service.GenerateAuthorizationUrl(MarketplaceType.MercadoLivre, "https://tunnel.test/cb", "s"));

        Assert.Equal(MarketplaceAuthService.MercadoLivreMissingCredentialsMessage, ex.Message);
    }

    [Fact]
    public async Task ExchangeMercadoLivreCodeAsync_ShouldPostAuthorizationCodeGrant()
    {
        string? body = null;
        HttpRequestMessage? captured = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            captured = request;
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"access_token":"APP_USR-1","token_type":"Bearer","expires_in":21600,"refresh_token":"TG-r","user_id":99}""")
            };
        });
        _httpClientFactory
            .Setup(f => f.CreateClient("Ofertio.MarketplaceOAuth"))
            .Returns(new HttpClient(handler));
        var service = CreateMercadoLivreService(new MercadoLivreIntegrationOptions
        {
            AppId = "123456",
            SecretKey = "secret"
        });

        var result = await service.ExchangeMercadoLivreCodeAsync(
            "TG-code",
            "https://tunnel.test/integracoes/oauth/callback");

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("APP_USR-1", result.AccessToken);
        Assert.Equal("TG-r", result.RefreshToken);
        Assert.True(result.ExpiresAt > DateTime.UtcNow.AddHours(5));
        Assert.Equal("https://api.mercadolibre.com/oauth/token", captured!.RequestUri!.AbsoluteUri);
        Assert.Contains("grant_type=authorization_code", body);
        Assert.Contains("client_id=123456", body);
        Assert.Contains("client_secret=secret", body);
        Assert.Contains("code=TG-code", body);
        Assert.Contains("redirect_uri=https%3A%2F%2Ftunnel.test%2Fintegracoes%2Foauth%2Fcallback", body);
    }

    [Fact]
    public async Task ExchangeMercadoLivreCodeAsync_ShouldFailWithoutCallingApi_WhenCredentialsAreMissing()
    {
        var service = CreateMercadoLivreService(new MercadoLivreIntegrationOptions());

        var result = await service.ExchangeMercadoLivreCodeAsync("TG-code", "https://tunnel.test/cb");

        Assert.False(result.Success);
        Assert.Equal(MarketplaceAuthService.MercadoLivreMissingCredentialsMessage, result.ErrorMessage);
        _httpClientFactory.Verify(factory => factory.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CallbackAndGenerateTokensAsync_ShouldReturnFailure_WhenCodeIsEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CallbackAndGenerateTokensAsync(
            MarketplaceType.Shopee,
            "",
            "shop-1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("inválido", result.Descricao, StringComparison.OrdinalIgnoreCase);
    }
}

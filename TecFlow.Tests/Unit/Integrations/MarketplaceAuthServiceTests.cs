using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;
using TecFlow.Infrastructure.Services.Integrations.Auth;
using TecFlow.Tests.Helpers;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.Integrations;

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
            "https://app.tecflow.test/callback",
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
            "https://app.tecflow.test/callback",
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
            .Setup(f => f.CreateClient("TecFlow.MarketplaceOAuth"))
            .Returns(client);

        // Act
        var token = await service.GetValidTokenAsync(shopId, MarketplaceType.TikTokShop);

        // Assert
        Assert.Equal("new-token", token);
        _tokenRepository.Verify(r => r.UpsertAsync(It.Is<MarketplaceToken>(t => t.AccessToken == "new-token")), Times.Once);
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

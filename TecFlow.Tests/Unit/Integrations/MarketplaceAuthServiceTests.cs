using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database.MultiTenancy;
using TecFlow.Infrastructure.Services.Integrations.Auth;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.Integrations;

public class MarketplaceAuthServiceTests
{
    private readonly Mock<IMarketplaceTokenRepository> _tokenRepository = new();
    private readonly Mock<IMarketplaceAccountRepository> _accountRepository = new();
    private readonly Mock<ICurrentTenantService> _currentTenant = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly MarketplaceSignatureService _signatureService = new();

    public MarketplaceAuthServiceTests()
    {
        _currentTenant.Setup(t => t.TenantId).Returns(Guid.NewGuid());
    }

    private MarketplaceAuthService CreateService() =>
        new(
            _tokenRepository.Object,
            _accountRepository.Object,
            _currentTenant.Object,
            _signatureService,
            _httpClientFactory.Object,
            MarketplaceTestOptionsFactory.TikTokOptions(),
            MarketplaceTestOptionsFactory.ShopeeOptions(),
            NullLogger<MarketplaceAuthService>.Instance);

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
            "https://app.tecflow.test/callback");

        // Assert
        Assert.Contains($"partner_id={MarketplaceTestOptionsFactory.ShopeePartnerId}", url);
        Assert.Contains("sign=", url);
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

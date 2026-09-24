using Moq;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Infrastructure.Services.Integrations;

namespace TecFlow.Tests.Unit.Integrations;

public class MarketplaceAccountMappingTests
{
    [Fact]
    public void MarketplaceAccountService_ShouldMapLegacyNullCredentials_WithoutNullReferenceException()
    {
        var account = new MarketplaceAccount
        {
            Id = 42,
            UserId = "7",
            TenantId = Guid.NewGuid(),
            MarketplaceType = MarketplaceType.Shopee,
            IsActive = true,
            ShopId = null,
            TrackingId = null,
            AffiliateTrackingId = null,
            AppKey = null,
            AppSecret = null,
            AccessToken = null,
            FriendlyName = null,
            ShopName = null,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        var service = CreateService();

        var dto = service.MapToDto(account);
        var convert = service.MapToConvertLinkResponse(account);

        Assert.NotNull(dto);
        Assert.Equal(string.Empty, dto.ShopId);
        Assert.Equal(string.Empty, dto.TrackingId);
        Assert.Equal(string.Empty, dto.AffiliateTrackingId);
        Assert.Equal(string.Empty, dto.AppKey);
        Assert.Equal(string.Empty, dto.FriendlyName);
        Assert.NotNull(convert);
        Assert.Equal(string.Empty, convert.ShopId);
        Assert.Equal(string.Empty, convert.TrackingId);
        Assert.True(convert.Status);
    }

    [Fact]
    public void MarketplaceAccountService_ShouldPreferTrackingIdWhenAffiliateColumnIsNull()
    {
        var account = new MarketplaceAccount
        {
            Id = 43,
            UserId = "7",
            ShopId = "ul-7-loja-homolog",
            TrackingId = "6512300000",
            AffiliateTrackingId = null,
            AppKey = null,
            AppSecret = null,
            FriendlyName = "Loja Homolog",
            MarketplaceType = MarketplaceType.Shopee,
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };

        var dto = CreateService().MapToDto(account);

        Assert.Equal("6512300000", dto.TrackingId);
        Assert.Equal("6512300000", dto.AffiliateTrackingId);
        Assert.Equal("ul-7-loja-homolog", dto.ShopId);
        Assert.Equal(43, dto.Id);
    }

    [Fact]
    public async Task InativarContaAsync_ShouldSetInactiveById()
    {
        var accounts = new Mock<IMarketplaceAccountRepository>();
        accounts.Setup(repository => repository.SetInactiveByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ok = await CreateService(accounts).InativarContaAsync(42);

        Assert.True(ok);
        accounts.Verify(repository => repository.SetInactiveByIdAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static MarketplaceAccountService CreateService(Mock<IMarketplaceAccountRepository>? accounts = null) =>
        new(
            new Mock<ITenantProvisioningService>().Object,
            new Mock<IUserAccountRepository>().Object,
            (accounts ?? new Mock<IMarketplaceAccountRepository>()).Object);
}

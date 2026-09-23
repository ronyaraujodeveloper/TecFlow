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
            TrackingId = "18325850271",
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

        Assert.Equal("18325850271", dto.TrackingId);
        Assert.Equal("18325850271", dto.AffiliateTrackingId);
        Assert.Equal("ul-7-loja-homolog", dto.ShopId);
    }

    private static MarketplaceAccountService CreateService() =>
        new(
            new Mock<ITenantProvisioningService>().Object,
            new Mock<IUserAccountRepository>().Object);
}

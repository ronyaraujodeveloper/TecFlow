using TecFlow.Business.Service.Radar;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Tests.Unit.Radar;

public class LiveSearchRulesTests
{
    [Fact]
    public void Matches_ShouldFilterKeywordPriceCouponAndStore()
    {
        Assert.True(LiveSearchRules.Matches(
            "Fone Bluetooth", 89m, "OFF10", "Shopee", MarketplaceType.Shopee,
            "fone", 50m, 100m, true, "shopee"));
        Assert.False(LiveSearchRules.Matches(
            "Fone Bluetooth", 89m, null, "Shopee", MarketplaceType.Shopee,
            "fone", null, null, true, null));
        Assert.False(LiveSearchRules.Matches(
            "Fone Bluetooth", 89m, "OFF10", "Shopee", MarketplaceType.Shopee,
            "cadeira", null, null, null, null));
        Assert.False(LiveSearchRules.Matches(
            "Fone Bluetooth", 89m, "OFF10", "Shopee", MarketplaceType.Shopee,
            null, null, null, null, "amazon"));
    }

    [Fact]
    public void IsCheaper_AndMapListingStatus()
    {
        Assert.True(LiveSearchRules.IsCheaper(100m, 80m));
        Assert.False(LiveSearchRules.IsCheaper(80m, 80m));
        Assert.Equal(GroupOfferStatuses.Esgotado, LiveSearchRules.MapListingStatus("paused", true));
        Assert.Equal(GroupOfferStatuses.Ativo, LiveSearchRules.MapListingStatus("active", true));
        Assert.Equal(GroupOfferStatuses.Esgotado, LiveSearchRules.MapListingStatus("active", false));
    }
}

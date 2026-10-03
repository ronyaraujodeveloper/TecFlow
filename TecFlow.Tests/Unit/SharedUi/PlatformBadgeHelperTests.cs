using TecFlow.Core.Enums;
using TecFlow.SharedUi.Helpers;

namespace TecFlow.Tests.Unit.SharedUi;

public class PlatformBadgeHelperTests
{
    [Theory]
    [InlineData(MarketplaceType.Amazon, "marketplace-platform-badge--amazon", "#FF9900")]
    [InlineData(MarketplaceType.MercadoLivre, "marketplace-platform-badge--mercadolivre", "#FFE600")]
    [InlineData(MarketplaceType.Shopee, "marketplace-platform-badge--shopee", "#EE4D2D")]
    [InlineData(MarketplaceType.MagazineLuiza, "marketplace-platform-badge--magalu", "#0086FF")]
    [InlineData(MarketplaceType.TikTokShop, "marketplace-platform-badge--tiktok", "#000000")]
    public void GetPlatformBadgeClass_ShouldUseOfficialColors(
        MarketplaceType platform,
        string expectedClass,
        string expectedColor)
    {
        Assert.Equal(expectedClass, PlatformBadgeHelper.GetPlatformBadgeClass(platform));
        Assert.Equal(expectedColor, PlatformBadgeHelper.GetPlatformColor(platform));
    }

    [Theory]
    [InlineData("Hotmart", "marketplace-platform-badge--hotmart", "#FF5200")]
    [InlineData("Braip", "marketplace-platform-badge--braip", "#12B76A")]
    [InlineData("Outros", "marketplace-platform-badge--default", "#6B7280")]
    public void GetPlatformBadgeClass_ShouldResolveAliases(
        string name,
        string expectedClass,
        string expectedColor)
    {
        Assert.Equal(expectedClass, PlatformBadgeHelper.GetPlatformBadgeClass(name));
        Assert.Equal(expectedColor, PlatformBadgeHelper.GetPlatformColor(name));
    }
}

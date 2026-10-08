using TecFlow.Business.Service.Radar;
using TecFlow.Core.Enums;

namespace TecFlow.Tests.Unit.Radar;

public class ProductSkuRulesTests
{
    [Fact]
    public void TryExtract_ShouldNormalizeMercadoLivreMlb()
    {
        Assert.True(ProductSkuRules.TryExtract(
            "https://www.mercadolivre.com.br/item/MLB-1234567890",
            MarketplaceType.MercadoLivre,
            out var platform,
            out var sku));
        Assert.Equal(nameof(MarketplaceType.MercadoLivre), platform);
        Assert.Equal("MLB1234567890", sku);
    }

    [Fact]
    public void TryExtract_ShouldJoinShopeeShopAndItem()
    {
        Assert.True(ProductSkuRules.TryExtract(
            "https://shopee.com.br/product/999999/888888",
            MarketplaceType.Shopee,
            out var platform,
            out var sku));
        Assert.Equal(nameof(MarketplaceType.Shopee), platform);
        Assert.Equal("999999:888888", sku);
    }

    [Fact]
    public void TryExtract_ShouldReadAmazonAsin()
    {
        Assert.True(ProductSkuRules.TryExtract(
            "https://www.amazon.com.br/dp/B08N5WRWNW",
            MarketplaceType.Amazon,
            out var platform,
            out var sku));
        Assert.Equal(nameof(MarketplaceType.Amazon), platform);
        Assert.Equal("B08N5WRWNW", sku);
    }
}

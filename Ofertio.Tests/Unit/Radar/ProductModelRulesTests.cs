using Ofertio.Business.Dto;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Enums;

namespace Ofertio.Tests.Unit.Radar;

public class ProductModelRulesTests
{
    [Theory]
    [InlineData("Smart TV Philips 43 43PFG6825/78", "Philips 43PFG6825")]
    [InlineData("tv philips 43pfg6825 4k", "Philips 43PFG6825")]
    [InlineData("Smart TV Philips 43", "Philips")]
    [InlineData("Capa de silicone", "")]
    [InlineData(null, "")]
    public void ExtractProductModel_ShouldKeepBrandAndCodeWithoutRegionSuffix(string? title, string expected)
    {
        Assert.Equal(expected, ProductModelRules.ExtractProductModel(title));
    }

    [Fact]
    public void SameProductModel_ShouldMatchTheCodeAcrossDifferentTitles()
    {
        Assert.True(ProductModelRules.SameProductModel(
            "Smart TV Philips 43 43PFG6825/78",
            "TV LED Philips 43PFG6825 4K"));
        Assert.False(ProductModelRules.SameProductModel(
            "Smart TV Philips 43 43PFG6825/78",
            "Smart TV Samsung UN55AU7700"));
    }

    [Fact]
    public void IsComparableOffer_WithoutCode_ShouldUseSimilarTitles()
    {
        Assert.True(ProductModelRules.IsComparableOffer(
            "Fone Bluetooth Philips TAH",
            "Fone Bluetooth Philips TAH original"));
        Assert.False(ProductModelRules.IsComparableOffer(
            "Smart TV Philips 43PFG6825",
            "Smart TV Samsung UN55AU7700"));
    }
}

public class OfferIndexRulesTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(15, false)]
    public void NeedsOfficialRefresh_ShouldFollowMasterCacheWindow(int cacheMinutes, bool expectedWhenRecent)
    {
        var now = new DateTime(2026, 10, 21, 15, 0, 0, DateTimeKind.Utc);
        var recent = now.AddMinutes(-5);

        Assert.Equal(expectedWhenRecent, OfferIndexRules.NeedsOfficialRefresh(recent, now, cacheMinutes));
        Assert.True(OfferIndexRules.NeedsOfficialRefresh(now.AddMinutes(-20), now, 15));
        Assert.True(OfferIndexRules.NeedsOfficialRefresh(null, now, 15));
    }

    [Fact]
    public void MatchesTerm_ShouldAcceptIndexedSearchOrProductText()
    {
        Assert.True(OfferIndexRules.MatchesTerm("games", "games", "Controle", null));
        Assert.True(OfferIndexRules.MatchesTerm("games", "outro", "Headset Games RGB", null));
        Assert.False(OfferIndexRules.MatchesTerm("casa", "games", "Headset", "AB12CD"));
    }

    [Fact]
    public void BuildComparisonTable_ShouldKeepReferenceHighlightLowestAndDropOtherModels()
    {
        var reference = new StorePriceComparisonRowDto
        {
            ProductName = "Smart TV Philips 43 43PFG6825/78",
            SourceUrl = "https://loja/referencia",
            PlatformName = "Mercado Livre",
            Price = 2100m
        };
        var rows = OfferIndexRules.BuildComparisonTable(
            reference,
            [
                new StorePriceComparisonRowDto
                {
                    ProductName = "TV Philips 43PFG6825",
                    SourceUrl = "https://loja/amazon",
                    PlatformName = "Amazon",
                    Price = 1800m,
                    Shipping = "Frete grátis"
                },
                new StorePriceComparisonRowDto
                {
                    ProductName = "Smart TV Samsung UN55AU7700",
                    SourceUrl = "https://loja/samsung",
                    Price = 1000m
                },
                new StorePriceComparisonRowDto
                {
                    ProductName = "Philips 43PFG6825",
                    SourceUrl = "https://loja/referencia",
                    PlatformName = "Mercado Livre",
                    Price = 2000m,
                    CommissionPercent = 8m
                }
            ],
            20);

        Assert.Equal(2, rows.Count);
        Assert.Equal(1800m, rows[0].Price);
        Assert.True(rows[0].IsLowestPrice);
        Assert.Contains(rows, row => row.IsReference && row.Price == 2000m && row.CommissionPercent == 8m);
        Assert.DoesNotContain(rows, row => row.SourceUrl.Contains("samsung", StringComparison.OrdinalIgnoreCase));
        Assert.False(rows.Single(row => row.IsReference).IsLowestPrice);
    }
}

using Ofertio.Business.Service.Groups;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;

namespace Ofertio.Tests.Unit.Groups;

public class OfferPlatformFilterRulesTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("  ", null)]
    [InlineData("MercadoLivre", "MercadoLivre")]
    [InlineData("shopee", "Shopee")]
    [InlineData("Magalu", "MagazineLuiza")]
    [InlineData("MagazineLuiza", "MagazineLuiza")]
    [InlineData("Ponto", "CasasBahia")]
    [InlineData("Kabum", "Kabum")]
    [InlineData("4", null)]
    [InlineData("Inexistente", null)]
    public void Normalize_ShouldMapKnownPlatformsAndRejectUnknown(string? input, string? expected)
    {
        Assert.Equal(expected, OfferPlatformFilterRules.Normalize(input));
    }

    [Fact]
    public void BuildOptions_ShouldListOnlyConnectedPlatformsInDisplayOrder()
    {
        var options = OfferPlatformFilterRules.BuildOptions(
            [MarketplaceType.Amazon, MarketplaceType.MercadoLivre, MarketplaceType.CasasBahia, MarketplaceType.Amazon],
            [MarketplaceType.Shopee]);

        Assert.Equal(["MercadoLivre", "Amazon", "CasasBahia"], options.Select(option => option.Value).ToArray());
        Assert.Equal(["Mercado Livre", "Amazon", "Casas Bahia / Ponto"], options.Select(option => option.Label).ToArray());
    }

    [Fact]
    public void BuildOptions_ShouldUseDetectedPlatformsWhenNoneConnected()
    {
        var options = OfferPlatformFilterRules.BuildOptions([], [MarketplaceType.Kabum, MarketplaceType.Shopee]);

        Assert.Equal(["Shopee", "Kabum"], options.Select(option => option.Value).ToArray());
    }

    [Fact]
    public void BuildOptions_ShouldFallBackToDefaultPlatforms()
    {
        var options = OfferPlatformFilterRules.BuildOptions(null, [(MarketplaceType)99]);

        Assert.Equal(["MercadoLivre", "Shopee"], options.Select(option => option.Value).ToArray());
    }

    [Fact]
    public void BuildOptions_ValuesShouldRoundTripThroughNormalize()
    {
        var options = OfferPlatformFilterRules.BuildOptions(Enum.GetValues<MarketplaceType>());

        Assert.Equal(Enum.GetValues<MarketplaceType>().Length, options.Count);
        Assert.All(options, option => Assert.Equal(option.Value, OfferPlatformFilterRules.Normalize(option.Value)));
    }

    [Fact]
    public void Apply_ShouldKeepEverythingWhenNoPlatformSelected()
    {
        var offers = Sample();

        Assert.Equal(offers.Length, OfferPlatformFilterRules.Apply(offers.AsQueryable(), null).Count());
        Assert.Equal(offers.Length, OfferPlatformFilterRules.Apply(offers.AsQueryable(), "Desconhecida").Count());
    }

    [Fact]
    public void Apply_ShouldFilterBySelectedPlatform()
    {
        var result = OfferPlatformFilterRules.Apply(Sample().AsQueryable(), "Magalu").ToList();

        var offer = Assert.Single(result);
        Assert.Equal(MarketplaceType.MagazineLuiza, offer.PlatformType);
    }

    private static GroupCapturedMessage[] Sample() =>
    [
        new() { PlatformType = MarketplaceType.MercadoLivre },
        new() { PlatformType = MarketplaceType.Shopee },
        new() { PlatformType = MarketplaceType.MagazineLuiza },
        new() { PlatformType = MarketplaceType.Kabum },
        new() { PlatformType = null }
    ];
}

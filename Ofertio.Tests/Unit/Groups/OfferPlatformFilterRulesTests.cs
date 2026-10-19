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
    [InlineData("outros", "Outros")]
    [InlineData("Kabum", null)]
    [InlineData("4", null)]
    [InlineData("Inexistente", null)]
    public void Normalize_ShouldMapKnownPlatformsAndRejectOthers(string? input, string? expected)
    {
        Assert.Equal(expected, OfferPlatformFilterRules.Normalize(input));
    }

    [Fact]
    public void Options_ShouldListRequestedMarketplacesAndOthers()
    {
        Assert.Equal(
            ["Mercado Livre", "Shopee", "Amazon", "Magazine Luiza", "AliExpress", "Casas Bahia / Ponto", "Outros"],
            OfferPlatformFilterRules.Options.Select(option => option.Label).ToArray());
        Assert.All(
            OfferPlatformFilterRules.Options,
            option => Assert.Equal(option.Value, OfferPlatformFilterRules.Normalize(option.Value)));
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

    [Fact]
    public void Apply_OthersShouldReturnUnlistedOrUnknownPlatforms()
    {
        var result = OfferPlatformFilterRules.Apply(Sample().AsQueryable(), OfferPlatformFilterRules.OthersFilter)
            .Select(item => item.PlatformType)
            .ToList();

        Assert.Equal([MarketplaceType.Kabum, null], result);
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

using TecFlow.Business.Service.Radar;
using TecFlow.Core.Enums;

namespace TecFlow.Tests.Unit.Radar;

public class AffiliateMiningRulesTests
{
    [Fact]
    public void ExtractCoupon_ShouldReadCodeFromOfferText()
    {
        var coupon = AffiliateMiningRules.ExtractCoupon("Use o cupom CASA10 e ganhe desconto");
        Assert.Equal("CASA10", coupon);
    }

    [Fact]
    public void TitlesLookAlike_ShouldMatchNormalizedNames()
    {
        Assert.True(AffiliateMiningRules.TitlesLookAlike(
            "Cadeira Gamer Pro Oficial Frete Grátis",
            "Cadeira Gamer Pro Preta"));
    }

    [Fact]
    public void MatchesTicket_ShouldRespectRange()
    {
        Assert.False(AffiliateMiningRules.MatchesTicket(20m, 50m, 200m));
        Assert.True(AffiliateMiningRules.MatchesTicket(99m, 50m, 200m));
    }

    [Fact]
    public void ComputeAttractivenessScore_ShouldRewardDropAndCoupon()
    {
        var score = AffiliateMiningRules.ComputeAttractivenessScore(80m, 100m, true, true, true, true);
        Assert.True(score >= 70);
    }

    [Fact]
    public void ResolveSearchPlatforms_ShouldKeepOnlyActivePreferredStores()
    {
        var platforms = AffiliateMiningRules.ResolveSearchPlatforms(
            [MarketplaceType.Shopee, MarketplaceType.Kabum],
            restrictToActiveStores: true);

        Assert.Equal(MarketplaceType.Shopee, Assert.Single(platforms));
    }
}

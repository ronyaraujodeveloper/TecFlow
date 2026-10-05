using TecFlow.Business.Service.Radar;

namespace TecFlow.Tests.Unit.Radar;

public class PreFlightRulesTests
{
    [Fact]
    public void IsInLookahead_ShouldAcceptCampaignWithinFifteenMinutes()
    {
        var now = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
        Assert.True(PreFlightRules.IsInLookahead(now.AddMinutes(10), now));
        Assert.False(PreFlightRules.IsInLookahead(now.AddMinutes(20), now));
    }

    [Fact]
    public void IsPriceIncrease_ShouldDetectHigherCurrentPrice()
    {
        Assert.True(PreFlightRules.IsPriceIncrease(49m, 89m));
        Assert.False(PreFlightRules.IsPriceIncrease(89m, 49m));
        Assert.False(PreFlightRules.IsPriceIncrease(null, 89m));
    }

    [Fact]
    public void BuildPauseMessage_ShouldExplainPriceChange()
    {
        var message = PreFlightRules.BuildPauseMessage(PreFlightRules.AlertPriceUp, 49m, 89m, null);
        Assert.Contains("49", message, StringComparison.Ordinal);
        Assert.Contains("89", message, StringComparison.Ordinal);
        Assert.StartsWith("Cancelado:", message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReplaceFirstUrl_ShouldSwapOnlyTheFirstLink()
    {
        var updated = PreFlightRules.ReplaceFirstUrl(
            "Oferta https://loja.com/a e https://loja.com/b",
            "https://barato.com/x");

        Assert.Contains("https://barato.com/x", updated, StringComparison.Ordinal);
        Assert.Contains("https://loja.com/b", updated, StringComparison.Ordinal);
        Assert.DoesNotContain("https://loja.com/a", updated, StringComparison.Ordinal);
    }
}

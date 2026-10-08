using TecFlow.Business.Service.Radar;

namespace TecFlow.Tests.Unit.Radar;

public class PriceHistoryRulesTests
{
    [Fact]
    public void MinAndAverage_ShouldUseOnlyWindow()
    {
        var now = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
        decimal[] prices = [100m, 80m, 40m];
        DateTime[] dates =
        [
            now.AddDays(-10),
            now.AddDays(-5),
            now.AddDays(-100)
        ];

        Assert.Equal(80m, PriceHistoryRules.MinInWindow(prices, dates, now, 30));
        Assert.Equal(90m, PriceHistoryRules.AverageInWindow(prices, dates, now, 30));
        Assert.True(PriceHistoryRules.IsLowestInWindow(80m, 80m));
        Assert.Equal(10m, PriceHistoryRules.SavingsVersusAverage(80m, 90m));
    }

    [Fact]
    public void ShouldSkipDuplicate_ShouldIgnoreSamePriceWithinSixHours()
    {
        var now = DateTime.UtcNow;
        Assert.True(PriceHistoryRules.ShouldSkipDuplicate(49m, now.AddHours(-1), 49m, now));
        Assert.False(PriceHistoryRules.ShouldSkipDuplicate(49m, now.AddHours(-7), 49m, now));
        Assert.False(PriceHistoryRules.ShouldSkipDuplicate(49m, now.AddHours(-1), 39m, now));
    }
}

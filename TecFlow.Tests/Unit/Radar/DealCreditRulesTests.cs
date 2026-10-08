using TecFlow.Business.Service.Radar;

namespace TecFlow.Tests.Unit.Radar;

public class DealCreditRulesTests
{
    [Fact]
    public void PriceDropPercent_ShouldDetectFifteenPercentDrop()
    {
        Assert.Equal(20m, DealCreditRules.PriceDropPercent(100m, 80m));
        Assert.True(DealCreditRules.IsSuperAchado(15m, 2));
        Assert.False(DealCreditRules.IsSuperAchado(14.9m, 10));
        Assert.False(DealCreditRules.IsSuperAchado(20m, 1));
    }

    [Fact]
    public void TryConsume_ShouldSpendDailyBeforePurchased()
    {
        Assert.True(DealCreditRules.TryConsume(2, 10, 1, out var daily, out var purchased));
        Assert.Equal(1, daily);
        Assert.Equal(10, purchased);
        Assert.True(DealCreditRules.TryConsume(0, 10, 1, out daily, out purchased));
        Assert.Equal(0, daily);
        Assert.Equal(9, purchased);
        Assert.False(DealCreditRules.TryConsume(0, 0, 1, out _, out _));
    }

    [Fact]
    public void ShouldGrantDaily_ShouldResetAtUtcMidnight()
    {
        var now = new DateTime(2026, 10, 7, 0, 5, 0, DateTimeKind.Utc);
        Assert.True(DealCreditRules.ShouldGrantDaily(now.AddDays(-1), now));
        Assert.False(DealCreditRules.ShouldGrantDaily(now, now));
        Assert.True(DealCreditRules.IsValidPack(50));
        Assert.False(DealCreditRules.IsValidPack(7));
    }
}

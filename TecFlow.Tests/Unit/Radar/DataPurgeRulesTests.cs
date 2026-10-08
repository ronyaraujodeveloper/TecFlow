using TecFlow.Business.Service.Radar;

namespace TecFlow.Tests.Unit.Radar;

public class DataPurgeRulesTests
{
    [Fact]
    public void DelayUntilNextDailyUtc_ShouldWaitUntilThreeAm()
    {
        var now = new DateTime(2026, 10, 8, 1, 0, 0, DateTimeKind.Utc);
        Assert.Equal(TimeSpan.FromHours(2), DataPurgeRules.DelayUntilNextDailyUtc(now));
        var after = new DateTime(2026, 10, 8, 3, 0, 0, DateTimeKind.Utc);
        Assert.Equal(TimeSpan.FromDays(1), DataPurgeRules.DelayUntilNextDailyUtc(after));
    }

    [Fact]
    public void Cutoffs_ShouldUseSevenAndFourteenDays()
    {
        var now = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(now.AddDays(-7), DataPurgeRules.MediaCutoff(now));
        Assert.Equal(now.AddDays(-14), DataPurgeRules.MessageCutoff(now));
        Assert.True(DataPurgeRules.IsPngOrJpeg("a.JPG"));
        Assert.True(DataPurgeRules.IsPngOrJpeg("b.png"));
        Assert.True(DataPurgeRules.IsPngOrJpeg("c.webp"));
        Assert.False(DataPurgeRules.IsPngOrJpeg("d.gif"));
    }
}

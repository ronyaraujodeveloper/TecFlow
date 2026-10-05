using TecFlow.Business.Service.Groups;

namespace TecFlow.Tests.Unit.Groups;

public class MonitoredGroupSyncRulesTests
{
    [Fact]
    public void BackgroundStartedMessage_ShouldTellUserWorkContinuesOffRequest()
    {
        Assert.Equal(
            "Sincronização iniciada! Os links estão sendo capturados em segundo plano.",
            MonitoredGroupSyncRules.BackgroundStartedMessage);
    }

    [Fact]
    public void ResolveHttpTimeout_ShouldBeAtLeastThreeMinutes()
    {
        Assert.Equal(TimeSpan.FromMinutes(3), MonitoredGroupSyncRules.ResolveHttpTimeout(30));
        Assert.Equal(TimeSpan.FromSeconds(240), MonitoredGroupSyncRules.ResolveHttpTimeout(240));
    }
}

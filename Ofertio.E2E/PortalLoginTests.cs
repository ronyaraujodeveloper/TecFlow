namespace Ofertio.E2E;

public class PortalLoginTests
{
    [Fact]
    public void RecordName_StartsWithTheE2EPrefix()
    {
        Assert.Equal("E2E-produto", E2ESettings.RecordName("produto"));
        Assert.Equal("E2E-campanha", E2ESettings.RecordName(" campanha "));
    }

    [RequiresE2EFact]
    public async Task Login_ReachesTheDashboard()
    {
        Assert.True(E2ESettings.TryLoad(out var settings));
        await using var session = await PortalSession.OpenAsync(settings!);
        await session.LoginAsync();
        Assert.Contains("/dashboard", session.Page.Url, StringComparison.OrdinalIgnoreCase);
    }
}

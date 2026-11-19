using Ofertio.Business.Service.Attribution;
using Xunit;

namespace Ofertio.Tests.Unit.Radar;

public sealed class FunnelAndRegionTests
{
    private static readonly DateOnly From = new(2026, 11, 10);
    private static readonly DateOnly To = new(2026, 11, 12);
    private static readonly IReadOnlyDictionary<string, AnalyticsGroupName> NoGroups = new Dictionary<string, AnalyticsGroupName>();

    private static DateTime Utc(int day, int hour) => new(2026, 11, day, hour, 0, 0, DateTimeKind.Utc);

    private static PublicEventRow Event(string kind, int day, int hour = 15, string? channel = null, bool bot = false, string ip = "1.1.1.***", string ua = "Chrome") =>
        new(Utc(day, hour), kind, channel, bot, ip, ua);

    private static AnalyticsClickRow Click(string? country, string? region, string? city, string ip = "1.1.1.***", bool bot = false) =>
        new(Utc(10, 15), bot, "WhatsApp", null, null, "Mobile", ip, "Chrome", Guid.NewGuid(), "Produto", null, "Mercado Livre", "97303724", "https://x.test/p/1", country, region, city);

    [Fact]
    public void Funnel_ShouldCountViewsUniqueConversionsAndJoinsAndIgnoreBots()
    {
        var events = new[]
        {
            Event("View", 10), Event("View", 10, hour: 16),                  // mesmo visitante no mesmo dia: 1 único
            Event("View", 11, ip: "2.2.2.***"), Event("View", 11, bot: true, ip: "9.9.9.***"),
            Event("Convert", 10), Event("Convert", 11, ip: "2.2.2.***"),
            Event("Join", 11, channel: "WhatsApp"), Event("Join", 12, channel: "Telegram", ip: "3.3.3.***"), Event("Join", 12, channel: "WhatsApp", bot: true)
        };

        var funnel = LinkAnalyticsCalculator.BuildFunnel(events, From, To);

        Assert.Equal(3, funnel.Views);
        Assert.Equal(2, funnel.UniqueViews);
        Assert.Equal(2, funnel.Conversions);
        Assert.Equal(2, funnel.JoinClicks);
        Assert.Equal(2, funnel.JoinByChannel.Count);
        Assert.All(funnel.JoinByChannel, item => Assert.Equal(50, item.Percent));
        Assert.True(funnel.HasData);
    }

    [Fact]
    public void Funnel_ShouldCompareWithThePreviousPeriodAndBeEmptyWithoutEvents()
    {
        var events = new[] { Event("View", 10), Event("View", 8), Event("View", 9, ip: "2.2.2.***"), Event("Convert", 8), Event("Join", 9, channel: "WhatsApp") };

        var funnel = LinkAnalyticsCalculator.BuildFunnel(events, From, To);
        var empty = LinkAnalyticsCalculator.BuildFunnel([], From, To);

        Assert.Equal(1, funnel.Views);
        Assert.Equal(2, funnel.PreviousViews);
        Assert.Equal(1, funnel.PreviousConversions);
        Assert.Equal(1, funnel.PreviousJoinClicks);
        Assert.False(empty.HasData);
    }

    [Fact]
    public void Overview_ShouldCarryTheFunnel()
    {
        var overview = LinkAnalyticsCalculator.BuildOverview(
            [], From, To, AnalyticsGrain.Day, 0, true, NoGroups, NoGroups, [Event("View", 10)]);

        Assert.Equal(1, overview.Funnel.Views);
    }

    [Fact]
    public void Regions_ShouldGroupByStateAndCityAndIgnoreBots()
    {
        var rows = new[]
        {
            Click("BR", "São Paulo", "Campinas"),
            Click("BR", "São Paulo", "Campinas", ip: "2.2.2.***"),
            Click("BR", "São Paulo", "Santos", ip: "3.3.3.***"),
            Click("BR", "Minas Gerais", "Belo Horizonte", ip: "4.4.4.***"),
            Click("BR", "Bahia", "Salvador", ip: "5.5.5.***", bot: true)
        };

        var overview = LinkAnalyticsCalculator.BuildOverview(rows, From, To, AnalyticsGrain.Day, 0, true, NoGroups, NoGroups);

        Assert.True(overview.HasRegionData);
        Assert.Equal("São Paulo · BR", overview.States[0].Label);
        Assert.Equal(3, overview.States[0].Clicks);
        Assert.DoesNotContain(overview.States, item => item.Label.StartsWith("Bahia"));
        Assert.Equal("Campinas · São Paulo", overview.Cities[0].Label);
        Assert.Equal(2, overview.Cities[0].Clicks);
    }

    [Fact]
    public void Regions_AbroadClicks_ShouldAppearAsExteriorWithTheCountryName()
    {
        var abroad = Click("PT", null, null) with { CountryName = "Portugal" };
        var rows = new[]
        {
            abroad,
            abroad with { Ip = "2.2.2.***" },
            Click("US", null, null, ip: "3.3.3.***"),
            Click("BR", "Bahia", "Salvador", ip: "4.4.4.***")
        };

        var overview = LinkAnalyticsCalculator.BuildOverview(rows, From, To, AnalyticsGrain.Day, 0, true, NoGroups, NoGroups);

        Assert.True(overview.HasRegionData);
        Assert.Equal("Exterior · Portugal", overview.States[0].Label);
        Assert.Equal(2, overview.States[0].Clicks);
        Assert.Contains(overview.States, item => item.Label == "Exterior · US");
        Assert.Single(overview.Cities);
    }

    [Fact]
    public void Regions_WithoutLocationData_ShouldBeEmptyAndFlaggedAsUnavailable()
    {
        var overview = LinkAnalyticsCalculator.BuildOverview(
            [Click(null, null, null)], From, To, AnalyticsGrain.Day, 0, true, NoGroups, NoGroups);

        Assert.False(overview.HasRegionData);
        Assert.Empty(overview.States);
        Assert.Empty(overview.Cities);
        Assert.Equal(1, overview.Summary.Clicks);
    }

    [Fact]
    public void Regions_ShouldNotExposeAnyIp()
    {
        var overview = LinkAnalyticsCalculator.BuildOverview(
            [Click("BR", "Bahia", "Salvador", ip: "187.12.34.***")], From, To, AnalyticsGrain.Day, 0, true, NoGroups, NoGroups);

        var all = string.Join('|', overview.States.Concat(overview.Cities).Select(item => item.Key + item.Label));
        Assert.DoesNotContain("187.12", all);
    }
}

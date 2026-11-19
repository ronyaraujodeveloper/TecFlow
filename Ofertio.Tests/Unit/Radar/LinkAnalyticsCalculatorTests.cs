using Ofertio.Business.Service.Attribution;
using Xunit;

namespace Ofertio.Tests.Unit.Radar;

public sealed class AnalyticsPeriodRulesTests
{
    [Fact]
    public void ToUtcRange_ShouldCoverTheBrazilianDay()
    {
        var (from, to) = AnalyticsPeriodRules.ToUtcRange(new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 10));

        Assert.Equal(new DateTime(2026, 11, 10, 3, 0, 0, DateTimeKind.Utc), from);
        Assert.Equal(new DateTime(2026, 11, 11, 3, 0, 0, DateTimeKind.Utc), to);
    }

    [Fact]
    public void ToLocalDate_ShouldTurnTheDayOverAtMidnightInBrasilia()
    {
        // 02:59 UTC ainda é 23:59 do dia anterior em Brasília; 03:00 UTC já é o dia seguinte.
        Assert.Equal(new DateOnly(2026, 11, 9), AnalyticsPeriodRules.ToLocalDate(new DateTime(2026, 11, 10, 2, 59, 0, DateTimeKind.Utc)));
        Assert.Equal(new DateOnly(2026, 11, 10), AnalyticsPeriodRules.ToLocalDate(new DateTime(2026, 11, 10, 3, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void Previous_ShouldHaveTheSameLengthRightBefore()
    {
        var (from, to) = AnalyticsPeriodRules.Previous(new DateOnly(2026, 11, 8), new DateOnly(2026, 11, 14));

        Assert.Equal(new DateOnly(2026, 11, 1), from);
        Assert.Equal(new DateOnly(2026, 11, 7), to);
    }

    [Theory]
    [InlineData("2026-11-18", "2026-11-16")] // quarta -> segunda
    [InlineData("2026-11-16", "2026-11-16")] // segunda
    [InlineData("2026-11-22", "2026-11-16")] // domingo -> segunda anterior
    public void BucketStart_Week_ShouldStartOnMonday(string date, string expected) =>
        Assert.Equal(DateOnly.Parse(expected), AnalyticsPeriodRules.BucketStart(DateOnly.Parse(date), AnalyticsGrain.Week));

    [Fact]
    public void Buckets_ShouldIncludeEmptyDaysSoTheChartHasNoGaps()
    {
        var buckets = AnalyticsPeriodRules.Buckets(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 7), AnalyticsGrain.Day);

        Assert.Equal(7, buckets.Count);
    }

    [Fact]
    public void Buckets_Month_ShouldCoverPartialMonths()
    {
        var buckets = AnalyticsPeriodRules.Buckets(new DateOnly(2026, 9, 20), new DateOnly(2026, 11, 3), AnalyticsGrain.Month);

        Assert.Equal([new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1), new DateOnly(2026, 11, 1)], buckets);
    }

    [Theory]
    [InlineData("2026-11-10", "2026-11-01", AnalyticsGrain.Day)]
    [InlineData("2026-01-01", "2026-12-31", AnalyticsGrain.Day)]
    [InlineData("2020-01-01", "2026-12-31", AnalyticsGrain.Month)]
    public void Validate_ShouldRejectInvalidRequests(string from, string to, AnalyticsGrain grain) =>
        Assert.NotNull(AnalyticsPeriodRules.Validate(DateOnly.Parse(from), DateOnly.Parse(to), grain));

    [Fact]
    public void Validate_ShouldAcceptAYearByMonth() =>
        Assert.Null(AnalyticsPeriodRules.Validate(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), AnalyticsGrain.Month));

    [Theory]
    [InlineData("dia", AnalyticsGrain.Day)]
    [InlineData("WEEK", AnalyticsGrain.Week)]
    [InlineData("mês", AnalyticsGrain.Month)]
    [InlineData("ano", AnalyticsGrain.Year)]
    public void TryParseGrain_ShouldAcceptPortugueseAndEnglish(string raw, AnalyticsGrain expected)
    {
        Assert.True(AnalyticsPeriodRules.TryParseGrain(raw, out var grain));
        Assert.Equal(expected, grain);
    }

    [Fact]
    public void WeekdayRow_ShouldStartOnMonday()
    {
        Assert.Equal(0, AnalyticsPeriodRules.WeekdayRow(new DateTime(2026, 11, 16)));
        Assert.Equal(6, AnalyticsPeriodRules.WeekdayRow(new DateTime(2026, 11, 22)));
    }
}

public sealed class LinkAnalyticsCalculatorTests
{
    private static readonly DateOnly From = new(2026, 11, 10);
    private static readonly DateOnly To = new(2026, 11, 12);
    private static readonly Guid ProductA = Guid.NewGuid();
    private static readonly Guid ProductB = Guid.NewGuid();
    private static readonly IReadOnlyDictionary<string, AnalyticsGroupName> NoGroups = new Dictionary<string, AnalyticsGroupName>();

    private static AnalyticsClickRow Click(
        DateTime utc,
        string channel = "WhatsApp",
        bool bot = false,
        string? groupKey = null,
        string? legacyGroup = null,
        Guid? product = null,
        string ip = "1.1.1.***",
        string ua = "Chrome",
        string device = "Mobile",
        string? name = "Produto A") =>
        new(utc, bot, channel, groupKey, legacyGroup, device, ip, ua, product ?? ProductA, name, null, "Mercado Livre", "97303724", "https://x.test/p/1");

    private static DateTime Utc(int day, int hour) => new(2026, 11, day, hour, 0, 0, DateTimeKind.Utc);

    private static Business.Dto.AnalyticsOverviewDto Build(params AnalyticsClickRow[] rows) =>
        LinkAnalyticsCalculator.BuildOverview(rows, From, To, AnalyticsGrain.Day, 5, true, NoGroups, NoGroups);

    [Fact]
    public void Overview_ShouldExcludeBotsFromClicksAndCountThemAsPreviews()
    {
        var result = Build(Click(Utc(10, 15)), Click(Utc(10, 16), bot: true), Click(Utc(11, 15), bot: true));

        Assert.Equal(1, result.Summary.Clicks);
        Assert.Equal(2, result.Summary.PreviewsFiltered);
        Assert.Equal(5, result.Summary.LinksGenerated);
    }

    [Fact]
    public void Overview_ShouldCountUniqueVisitorsPerIpAndBrowserPerDay()
    {
        var result = Build(
            Click(Utc(10, 15)),
            Click(Utc(10, 16)),                 // mesmo visitante, mesmo dia
            Click(Utc(11, 15)),                 // mesmo visitante, outro dia
            Click(Utc(10, 17), ip: "2.2.2.***"));

        Assert.Equal(4, result.Summary.Clicks);
        Assert.Equal(3, result.Summary.UniqueClicks);
    }

    [Fact]
    public void Overview_ShouldBucketByBrazilianDayAndFillEmptyDays()
    {
        // 10/11 02:30 UTC pertence ao dia 09 em Brasília (fora do período); 10/11 03:30 UTC é dia 10.
        var result = Build(Click(Utc(10, 2)), Click(Utc(10, 3)), Click(Utc(12, 14)));

        Assert.Equal(3, result.Series.Count);
        Assert.Equal(1, result.Series[0].Total);
        Assert.Equal(0, result.Series[1].Total);
        Assert.Equal(1, result.Series[2].Total);
    }

    [Fact]
    public void Overview_ShouldSplitSeriesByChannelAndOrderChannelsByUse()
    {
        var result = Build(
            Click(Utc(10, 15), channel: "Telegram"),
            Click(Utc(10, 16), channel: "WhatsApp"),
            Click(Utc(11, 16), channel: "WhatsApp"));

        Assert.Equal(["WhatsApp", "Telegram"], result.ChannelOrder);
        Assert.Equal(1, result.Series[0].ByChannel["Telegram"]);
        Assert.Equal(66.7, result.Channels.Single(item => item.Key == "WhatsApp").Percent);
    }

    [Fact]
    public void Overview_ShouldComparePreviousPeriodOfTheSameLength()
    {
        var result = Build(
            Click(Utc(10, 15)),
            Click(Utc(11, 15)),
            Click(Utc(8, 15)),                  // período anterior (07 a 09)
            Click(Utc(9, 15), ip: "9.9.9.***"));

        Assert.Equal(2, result.Summary.Clicks);
        Assert.Equal(2, result.Summary.PreviousClicks);
    }

    [Fact]
    public void Overview_ShouldFindTheBestDay()
    {
        var result = Build(Click(Utc(10, 15)), Click(Utc(11, 15)), Click(Utc(11, 16), ip: "3.3.3.***"));

        Assert.Equal(new DateOnly(2026, 11, 11), result.Summary.BestDay);
        Assert.Equal(2, result.Summary.BestDayClicks);
    }

    [Fact]
    public void Overview_TopProducts_ShouldRankByClicksAndKeepAtMostTen()
    {
        var rows = new List<AnalyticsClickRow>();
        for (var index = 0; index < 12; index++)
        {
            var id = Guid.NewGuid();
            for (var click = 0; click <= index; click++)
            {
                rows.Add(Click(Utc(10, 15), product: id, name: $"Produto {index}", ip: $"{index}.{click}.0.***"));
            }
        }

        var result = LinkAnalyticsCalculator.BuildOverview(rows, From, To, AnalyticsGrain.Day, 0, true, NoGroups, NoGroups);

        Assert.Equal(10, result.TopProducts.Count);
        Assert.Equal("Produto 11", result.TopProducts[0].Name);
        Assert.Equal(12, result.TopProducts[0].Clicks);
    }

    [Fact]
    public void Overview_Groups_ShouldResolveNamesAndNeverExposeTheRawGroupId()
    {
        var key = GroupKeyRules.Compute("s", 1, "WhatsApp", "120363025246125486@g.us");
        var byKey = new Dictionary<string, AnalyticsGroupName> { [key] = new("Achadinhos VIP", "WhatsApp") };
        var legacy = new Dictionary<string, AnalyticsGroupName> { ["120363025246125486@g.us"] = new("Achadinhos VIP", "WhatsApp") };

        var result = LinkAnalyticsCalculator.BuildOverview(
            [
                Click(Utc(10, 15), groupKey: key),
                Click(Utc(10, 16), legacyGroup: "120363025246125486@g.us", ip: "5.5.5.***"),
                Click(Utc(10, 17), groupKey: GroupKeyRules.Compute("s", 1, "WhatsApp", "removido@g.us"), ip: "6.6.6.***")
            ],
            From, To, AnalyticsGrain.Day, 0, true, byKey, legacy);

        Assert.Contains(result.Groups, item => item.Name == "Achadinhos VIP" && item.Clicks == 1 && item.Key == key);
        Assert.Contains(result.Groups, item => item.Name == "Achadinhos VIP" && item.Key == "legado");
        Assert.Contains(result.Groups, item => item.Name == "Grupo removido");
        Assert.DoesNotContain(result.Groups, item => item.Key.Contains('@'));
    }

    [Fact]
    public void Overview_Heatmap_ShouldUseBrazilianWeekdayAndHour()
    {
        // 16/11/2026 é segunda; 18:00 UTC = 15:00 em Brasília.
        var result = Build(Click(new DateTime(2026, 11, 16, 18, 0, 0, DateTimeKind.Utc)));

        var wide = LinkAnalyticsCalculator.BuildOverview(
            [Click(new DateTime(2026, 11, 16, 18, 0, 0, DateTimeKind.Utc))],
            new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 22), AnalyticsGrain.Day, 0, true, NoGroups, NoGroups);

        Assert.Equal(7, wide.Heatmap.Count);
        Assert.All(wide.Heatmap, line => Assert.Equal(24, line.Count));
        Assert.Equal(1, wide.Heatmap[0][15]);
        Assert.Equal(0, result.Summary.Clicks);
    }

    [Fact]
    public void ProductDetail_ShouldBuildSeriesAndShopsForOneProduct()
    {
        var rows = new[]
        {
            Click(Utc(10, 15), channel: "Telegram"),
            Click(Utc(11, 15), channel: "WhatsApp", ip: "7.7.7.***")
        };

        var detail = LinkAnalyticsCalculator.BuildProductDetail(rows, From, To, AnalyticsGrain.Day, NoGroups, NoGroups);

        Assert.Equal("Produto A", detail.Product.Name);
        Assert.Equal(2, detail.Summary.Clicks);
        Assert.Equal(3, detail.Series.Count);
        Assert.Single(detail.Shops);
        Assert.Equal(100, detail.Shops[0].Percent);
    }

    [Fact]
    public void Overview_WithoutClicks_ShouldReturnAnEmptyButWellFormedResult()
    {
        var result = Build();

        Assert.Equal(0, result.Summary.Clicks);
        Assert.Equal(3, result.Series.Count);
        Assert.Empty(result.TopProducts);
        Assert.Equal(7, result.Heatmap.Count);
        Assert.Null(result.Summary.BestDay);
    }
}

using Ofertio.Business.Service.Common;

namespace Ofertio.Tests.Unit.Common;

public class BrazilTimeZoneRulesTests
{
    [Theory]
    [InlineData("SP", -3)]
    [InlineData("rj", -3)]
    [InlineData("BA", -3)]
    [InlineData("AM", -4)]
    [InlineData("MT", -4)]
    [InlineData("MS", -4)]
    [InlineData("AC", -5)]
    public void Resolve_ShouldReturnStateOffset(string uf, int expectedOffset)
    {
        Assert.Equal(expectedOffset, BrazilTimeZoneRules.Resolve(uf).OffsetHours);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("XX")]
    [InlineData("SAO")]
    public void Resolve_ShouldFallBackToBrasilia(string? uf)
    {
        Assert.Same(BrazilTimeZoneRules.Brasilia, BrazilTimeZoneRules.Resolve(uf));
        Assert.Null(BrazilTimeZoneRules.NormalizeStateCode(uf));
    }

    [Fact]
    public void States_ShouldListAllTwentySevenUfs()
    {
        Assert.Equal(27, BrazilTimeZoneRules.States.Select(state => state.Code).Distinct().Count());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(11)]
    public void ToLocal_ShouldNotApplyDaylightSaving(int month)
    {
        var utc = new DateTime(2026, month, 15, 23, 0, 0, DateTimeKind.Utc);

        Assert.Equal(20, BrazilTimeZoneRules.ToLocal(utc, "SP").Hour);
        Assert.Equal(19, BrazilTimeZoneRules.ToLocal(utc, "AM").Hour);
        Assert.Equal(18, BrazilTimeZoneRules.ToLocal(utc, "AC").Hour);
    }

    [Fact]
    public void ToUtc_ShouldConvertSaoPauloEveningToUtc()
    {
        var utc = BrazilTimeZoneRules.ToUtc(new DateTime(2026, 10, 26, 20, 0, 0), "SP");

        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(new DateTime(2026, 10, 26, 23, 0, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void ToUtc_ShouldMoveAmazonasEveningToNextUtcDay()
    {
        var utc = BrazilTimeZoneRules.ToUtc(new DateTime(2026, 10, 26, 20, 0, 0), "AM");

        Assert.Equal(new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void ToUtc_ShouldIgnoreServerTimeZoneForLocalKind()
    {
        var wall = new DateTime(2026, 10, 26, 20, 0, 0, DateTimeKind.Local);

        Assert.Equal(new DateTime(2026, 10, 26, 23, 0, 0, DateTimeKind.Utc), BrazilTimeZoneRules.ToUtc(wall, null));
    }

    [Theory]
    [InlineData("SP")]
    [InlineData("AM")]
    [InlineData("AC")]
    [InlineData(null)]
    public void ToLocal_ShouldRoundTripWithToUtc(string? uf)
    {
        var utc = new DateTime(2026, 3, 1, 2, 30, 0, DateTimeKind.Utc);

        Assert.Equal(utc, BrazilTimeZoneRules.ToUtc(BrazilTimeZoneRules.ToLocal(utc, uf), uf));
    }

    [Fact]
    public void NormalizeUtc_ShouldTreatUnspecifiedAsUtc()
    {
        var value = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Unspecified);

        var normalized = BrazilTimeZoneRules.NormalizeUtc(value);

        Assert.Equal(DateTimeKind.Utc, normalized.Kind);
        Assert.Equal(12, normalized.Hour);
    }

    [Fact]
    public void DayRangeUtc_ShouldCoverTheClientDay()
    {
        var lateEveningUtc = new DateTime(2026, 10, 27, 1, 0, 0, DateTimeKind.Utc);

        var (start, end) = BrazilTimeZoneRules.DayRangeUtc(lateEveningUtc, "SP");

        Assert.Equal(new DateTime(2026, 10, 26, 3, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 10, 27, 3, 0, 0, DateTimeKind.Utc), end);
    }

    [Fact]
    public void DayRangeUtc_ShouldUseAcreOffset()
    {
        var (start, end) = BrazilTimeZoneRules.DayRangeUtc(new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc), "AC");

        Assert.Equal(new DateTime(2026, 10, 26, 5, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 10, 27, 5, 0, 0, DateTimeKind.Utc), end);
    }
}

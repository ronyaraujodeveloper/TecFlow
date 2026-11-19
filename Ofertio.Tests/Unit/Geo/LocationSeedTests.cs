using Ofertio.Data.Seed;
using Ofertio.Util.Address;
using Xunit;

namespace Ofertio.Tests.Unit.Geo;

/// <summary>Confere a carga embutida da migration AddLocationTables (lista do IBGE).</summary>
public sealed class LocationSeedTests
{
    [Fact]
    public void Countries_ShouldHaveUniqueIsoCodesIncludingBrazil()
    {
        var countries = LocationSeed.Countries();

        Assert.True(countries.Count >= 240);
        Assert.All(countries, row => Assert.Matches("^[A-Z]{2}$", row.IsoCode));
        Assert.Equal(countries.Count, countries.Select(row => row.IsoCode).Distinct().Count());
        Assert.Contains(countries, row => row is { IsoCode: "BR", Name: "Brasil" });
        Assert.All(countries, row => Assert.InRange(row.Name.Length, 2, 80));
    }

    [Fact]
    public void States_ShouldBeThe27BrazilianUfs()
    {
        var states = LocationSeed.States();

        Assert.Equal(27, states.Count);
        Assert.Equal(27, states.Select(row => row.Code).Distinct().Count());
        Assert.Contains(states, row => row is { Code: "SP", Name: "São Paulo", IbgeCode: 35 });
    }

    [Fact]
    public void Cities_ShouldPointToKnownStatesAndBeUniqueInsideEachState()
    {
        var codes = LocationSeed.States().Select(row => row.Code).ToHashSet();
        var cities = LocationSeed.Cities();

        Assert.True(cities.Count >= 5570);
        Assert.All(cities, row => Assert.Contains(row.StateCode, codes));
        Assert.All(cities, row => Assert.InRange(row.IbgeCode, 1_000_000, 9_999_999));
        Assert.All(cities, row => Assert.InRange(row.Name.Length, 2, 80));
        Assert.Equal(cities.Count, cities.Select(row => row.IbgeCode).Distinct().Count());
        Assert.Equal(cities.Count, cities.Select(row => (row.StateCode, LocationNameNormalizer.Normalize(row.Name))).Distinct().Count());
        Assert.Contains(cities, row => row is { IbgeCode: 3550308, StateCode: "SP", Name: "São Paulo" });
    }

    [Fact]
    public void InsertStatements_ShouldEscapeQuotesAndKeepAccents()
    {
        var sql = string.Join('\n', LocationSeed.InsertStatements());

        Assert.Contains("N'Alta Floresta D''Oeste'", sql);
        Assert.Contains("N'alta floresta d oeste'", sql);
        Assert.Contains("N'São Paulo'", sql);
    }
}

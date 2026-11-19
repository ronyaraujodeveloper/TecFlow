using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Geo;
using Ofertio.Core.Entities;
using Ofertio.Util.Address;
using Xunit;

namespace Ofertio.Tests.Unit.Geo;

public sealed class LocationMatchRulesTests
{
    private static readonly LocationSnapshot Snapshot = new(
        [
            new Country { Id = 1, IsoCode = "BR", Name = "Brasil" },
            new Country { Id = 2, IsoCode = "PT", Name = "Portugal" }
        ],
        [
            new State { Id = 10, CountryId = 1, Code = "SP", Name = "São Paulo", IbgeCode = 35 },
            new State { Id = 11, CountryId = 1, Code = "PI", Name = "Piauí", IbgeCode = 22 },
            new State { Id = 12, CountryId = 1, Code = "RS", Name = "Rio Grande do Sul", IbgeCode = 43 },
            new State { Id = 13, CountryId = 1, Code = "RO", Name = "Rondônia", IbgeCode = 11 }
        ],
        [
            City(100, 10, "São Paulo", 3550308),
            City(101, 10, "Campinas", 3509502),
            City(102, 11, "Bom Jesus", 2201903),
            City(103, 12, "Bom Jesus", 4302402),
            City(104, 13, "Alta Floresta D'Oeste", 1100015)
        ]);

    private static City City(int id, int stateId, string name, int ibge) =>
        new() { Id = id, StateId = stateId, Name = name, NormalizedName = LocationNameNormalizer.Normalize(name), IbgeCode = ibge };

    [Theory]
    [InlineData("São Paulo", "sao paulo")]
    [InlineData("  SÃO   PAULO ", "sao paulo")]
    [InlineData("Alta Floresta D'Oeste", "alta floresta d oeste")]
    [InlineData("alta floresta d’oeste", "alta floresta d oeste")]
    [InlineData("Embu-Guaçu", "embu guacu")]
    [InlineData(null, "")]
    public void Normalize_ShouldIgnoreAccentsCaseAndPunctuation(string? input, string expected) =>
        Assert.Equal(expected, LocationNameNormalizer.Normalize(input));

    [Fact]
    public void Resolve_ShouldMatchStateByCodeAndCityInsideTheState()
    {
        var ids = LocationMatchRules.Resolve(Snapshot, new GeoLocation("BR", "Sao Paulo", "campinas", "SP"));

        Assert.Equal(new LocationIds(1, 10, 101), ids);
    }

    [Fact]
    public void Resolve_WithoutCode_ShouldFallBackToTheStateNameIgnoringAccentsAndCase()
    {
        var ids = LocationMatchRules.Resolve(Snapshot, new GeoLocation("BR", "são paulo", "SÃO PAULO"));

        Assert.Equal(new LocationIds(1, 10, 100), ids);
    }

    [Fact]
    public void Resolve_ShouldAcceptTheNameWhereTheCodeShouldBe()
    {
        var ids = LocationMatchRules.Resolve(Snapshot, "BR", "Piauí", null, "Bom Jesus");

        Assert.Equal(new LocationIds(1, 11, 102), ids);
    }

    [Fact]
    public void Resolve_HomonymCityWithoutState_ShouldKeepOnlyTheCountry()
    {
        var ids = LocationMatchRules.Resolve(Snapshot, new GeoLocation("BR", null, "Bom Jesus"));

        Assert.Equal(new LocationIds(1, null, null), ids);
    }

    [Fact]
    public void Resolve_UniqueCityWithoutState_ShouldTakeTheStateFromTheCity()
    {
        var ids = LocationMatchRules.Resolve(Snapshot, new GeoLocation("BR", null, "Alta Floresta d'Oeste"));

        Assert.Equal(new LocationIds(1, 13, 104), ids);
    }

    [Fact]
    public void Resolve_UnknownCity_ShouldKeepCountryAndState()
    {
        var ids = LocationMatchRules.Resolve(Snapshot, new GeoLocation("BR", "São Paulo", "Cidade Inexistente", "SP"));

        Assert.Equal(new LocationIds(1, 10, null), ids);
    }

    [Fact]
    public void Resolve_Abroad_ShouldRecordOnlyTheCountry()
    {
        var ids = LocationMatchRules.Resolve(Snapshot, new GeoLocation("PT", "Lisboa", "Lisboa", "11"));

        Assert.Equal(new LocationIds(2, null, null), ids);
    }

    [Fact]
    public void CitiesOfState_ShouldListOnlyTheUfInAlphabeticalOrder()
    {
        var snapshot = new LocationSnapshot(
            [new Country { Id = 1, IsoCode = "BR", Name = "Brasil" }],
            [new State { Id = 10, CountryId = 1, Code = "SP", Name = "São Paulo", IbgeCode = 35 },
             new State { Id = 11, CountryId = 1, Code = "RJ", Name = "Rio de Janeiro", IbgeCode = 33 }],
            [City(1, 10, "São Paulo", 3550308), City(2, 10, "Águas de Lindóia", 3500501), City(3, 10, "Campinas", 3509502), City(4, 11, "Niterói", 3303302)]);

        Assert.Equal(["Águas de Lindóia", "Campinas", "São Paulo"], snapshot.CitiesOfState("sp").Select(city => city.Name));
        Assert.Empty(snapshot.CitiesOfState("XX"));
        Assert.Empty(snapshot.CitiesOfState(null));
    }

    [Fact]
    public void Resolve_UnknownCountryOrEmptyLocation_ShouldBeEmpty()
    {
        Assert.True(LocationMatchRules.Resolve(Snapshot, new GeoLocation("ZZ", "X", "Y")).IsEmpty);
        Assert.True(LocationMatchRules.Resolve(Snapshot, null).IsEmpty);
        Assert.True(LocationMatchRules.Resolve(LocationSnapshot.Empty, new GeoLocation("BR", "SP", "Campinas", "SP")).IsEmpty);
    }
}

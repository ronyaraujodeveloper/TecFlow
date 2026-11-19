using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Entities;
using Ofertio.Util.Address;

namespace Ofertio.Business.Service.Geo;

/// <summary>IDs de país, estado e cidade gravados no destino (clique, evento). Qualquer um pode ficar nulo.</summary>
public sealed record LocationIds(int? CountryId, int? StateId, int? CityId)
{
    public static readonly LocationIds Empty = new(null, null, null);

    public bool IsEmpty => CountryId is null && StateId is null && CityId is null;
}

/// <summary>
/// Fotografia das tabelas de país, estado e cidade em memória, com os índices de busca prontos.
/// São poucos registros (cerca de 250 países, 27 estados e 5.570 cidades), então cabe tudo em memória
/// e cada clique é resolvido sem ir ao banco.
/// </summary>
public sealed class LocationSnapshot
{
    public static readonly LocationSnapshot Empty = new([], [], []);

    private readonly Dictionary<string, Country> _countriesByIso = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Country> _countriesByName = new(StringComparer.Ordinal);
    private readonly Dictionary<(int CountryId, string Code), State> _statesByCode = new();
    private readonly Dictionary<(int CountryId, string Name), State> _statesByName = new();
    private readonly Dictionary<(int StateId, string Name), City> _citiesByName = new();
    private readonly Dictionary<(int CountryId, string Name), List<City>> _citiesByCountryName = new();

    public LocationSnapshot(IEnumerable<Country> countries, IEnumerable<State> states, IEnumerable<City> cities)
    {
        CountriesById = countries.ToDictionary(item => item.Id);
        StatesById = states.ToDictionary(item => item.Id);
        CitiesById = cities.ToDictionary(item => item.Id);

        foreach (var country in CountriesById.Values)
        {
            _countriesByIso.TryAdd(country.IsoCode.Trim(), country);
            _countriesByName.TryAdd(LocationNameNormalizer.Normalize(country.Name), country);
        }

        foreach (var state in StatesById.Values)
        {
            _statesByCode.TryAdd((state.CountryId, state.Code.Trim().ToUpperInvariant()), state);
            _statesByName.TryAdd((state.CountryId, LocationNameNormalizer.Normalize(state.Name)), state);
        }

        foreach (var city in CitiesById.Values)
        {
            var name = string.IsNullOrWhiteSpace(city.NormalizedName)
                ? LocationNameNormalizer.Normalize(city.Name)
                : city.NormalizedName;
            _citiesByName.TryAdd((city.StateId, name), city);

            if (StatesById.TryGetValue(city.StateId, out var state))
            {
                var key = (state.CountryId, name);
                if (!_citiesByCountryName.TryGetValue(key, out var list))
                {
                    _citiesByCountryName[key] = list = [];
                }

                list.Add(city);
            }
        }
    }

    public IReadOnlyDictionary<int, Country> CountriesById { get; }

    public IReadOnlyDictionary<int, State> StatesById { get; }

    public IReadOnlyDictionary<int, City> CitiesById { get; }

    public bool IsEmpty => CountriesById.Count == 0;

    /// <summary>Cidades de uma UF do Brasil em ordem alfabética (pt-BR), para o combo de cidade. UF desconhecida: lista vazia.</summary>
    public IReadOnlyList<City> CitiesOfState(string? stateCode)
    {
        var brazil = FindCountry(Country.BrazilIsoCode);
        var state = brazil is null ? null : FindStateByCode(brazil.Id, stateCode);
        if (state is null)
        {
            return [];
        }

        var culture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
        return CitiesById.Values
            .Where(city => city.StateId == state.Id)
            .OrderBy(city => city.Name, StringComparer.Create(culture, ignoreCase: true))
            .ToList();
    }

    internal Country? FindCountry(string? isoOrName)
    {
        if (string.IsNullOrWhiteSpace(isoOrName))
        {
            return null;
        }

        return _countriesByIso.GetValueOrDefault(isoOrName.Trim())
            ?? _countriesByName.GetValueOrDefault(LocationNameNormalizer.Normalize(isoOrName));
    }

    internal State? FindStateByCode(int countryId, string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : _statesByCode.GetValueOrDefault((countryId, code.Trim().ToUpperInvariant()));

    internal State? FindStateByName(int countryId, string? name) =>
        string.IsNullOrWhiteSpace(name) ? null : _statesByName.GetValueOrDefault((countryId, LocationNameNormalizer.Normalize(name)));

    internal City? FindCity(int stateId, string? name) =>
        string.IsNullOrWhiteSpace(name) ? null : _citiesByName.GetValueOrDefault((stateId, LocationNameNormalizer.Normalize(name)));

    internal IReadOnlyList<City> FindCitiesInCountry(int countryId, string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? []
            : _citiesByCountryName.GetValueOrDefault((countryId, LocationNameNormalizer.Normalize(name))) ?? [];
}

/// <summary>
/// Casa o texto que vem da localização por IP (ou de outra fonte) com as tabelas de referência.
/// País pelo código ISO; estado pela sigla e, como plano B, pelo nome sem acento; cidade sempre dentro do estado,
/// porque há nomes repetidos entre estados (Bom Jesus, Santa Luzia). O que não casar fica nulo.
/// </summary>
public static class LocationMatchRules
{
    public static LocationIds Resolve(LocationSnapshot snapshot, GeoLocation? location)
    {
        if (location is null || location.IsEmpty)
        {
            return LocationIds.Empty;
        }

        return Resolve(snapshot, location.Country, location.RegionCode, location.Region, location.City);
    }

    public static LocationIds Resolve(
        LocationSnapshot snapshot,
        string? country,
        string? stateCode,
        string? stateName,
        string? cityName)
    {
        var foundCountry = snapshot.FindCountry(country);
        if (foundCountry is null)
        {
            return LocationIds.Empty;
        }

        var state = snapshot.FindStateByCode(foundCountry.Id, stateCode)
            ?? snapshot.FindStateByName(foundCountry.Id, stateName)
            // Algumas bases mandam o nome no lugar da sigla e vice-versa.
            ?? snapshot.FindStateByCode(foundCountry.Id, stateName)
            ?? snapshot.FindStateByName(foundCountry.Id, stateCode);

        City? city = null;
        if (state is not null)
        {
            city = snapshot.FindCity(state.Id, cityName);
        }
        else
        {
            // Sem estado, a cidade só vale se o nome for único no país; aí o estado sai dela.
            var candidates = snapshot.FindCitiesInCountry(foundCountry.Id, cityName);
            if (candidates.Count == 1)
            {
                city = candidates[0];
                state = snapshot.StatesById.GetValueOrDefault(city.StateId);
            }
        }

        return new LocationIds(foundCountry.Id, state?.Id, city?.Id);
    }
}

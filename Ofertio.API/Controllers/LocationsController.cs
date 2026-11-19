using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Database.Filter;
using Ofertio.Util.Address;

namespace Ofertio.API.Controllers;

/// <summary>Tabelas de referência de localização (cidades por UF), servidas da memória.</summary>
[ApiController]
[Authorize]
[Route("api/localidades")]
public sealed class LocationsController : ControllerBase
{
    private readonly ILocationCatalog _locations;

    public LocationsController(ILocationCatalog locations)
    {
        _locations = locations;
    }

    [HttpGet("cidades")]
    public async Task<ActionResult<CityResponseDto>> GetCitiesAsync([FromQuery] CityFilter filter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filter.StateCode))
        {
            return BadRequest(new CityResponseDto { Status = false, Descricao = "Informe a UF." });
        }

        var snapshot = await _locations.GetSnapshotAsync(cancellationToken);
        var cities = snapshot.CitiesOfState(filter.StateCode);
        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            var term = LocationNameNormalizer.Normalize(filter.Name);
            cities = cities.Where(city => city.NormalizedName.Contains(term, StringComparison.Ordinal)).ToList();
        }

        return Ok(new CityResponseDto { Status = true, Descricao = "OK", DataList = cities.ToList() });
    }
}

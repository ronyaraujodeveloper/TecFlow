using Ofertio.Core.Entities;

namespace Ofertio.Business.Dto;

public class CityResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public City? Data { get; set; }
    public List<City>? DataList { get; set; }
}

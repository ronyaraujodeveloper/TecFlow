namespace Ofertio.Business.Dto;

public class CustomerDto
{
    public string Name { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Street { get; set; } = string.Empty;
    public string StreetNumber { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    /// <summary>Cidade da tabela de referência (api/localidades/estados/{uf}/cidades); a UF sai dela.</summary>
    public int? CityId { get; set; }
}

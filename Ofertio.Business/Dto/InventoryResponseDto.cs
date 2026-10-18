namespace Ofertio.Business.Dto;

public class InventoryResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public Ofertio.Core.Entities.Inventory? Data { get; set; }
    public List<Ofertio.Core.Entities.Inventory>? DataList { get; set; }
    public PagingInfoDto? Paging { get; set; }
}

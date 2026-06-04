namespace TecFlow.Business.Dto;

public class InventoryResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public TecFlow.Core.Entities.Inventory? Data { get; set; }
    public List<TecFlow.Core.Entities.Inventory>? DataList { get; set; }
    public PagingInfoDto? Paging { get; set; }
}

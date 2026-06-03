using TecFlow.Core.Entities;

namespace TecFlow.Business.Dto;

public class ItemResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public Item? Data { get; set; }
    public List<Item>? DataList { get; set; }
}

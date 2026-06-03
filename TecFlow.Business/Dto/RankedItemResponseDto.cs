using TecFlow.Core.Entities;

namespace TecFlow.Business.Dto;

public class RankedItemResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public RankedItem? Data { get; set; }
    public List<RankedItem>? DataList { get; set; }
}

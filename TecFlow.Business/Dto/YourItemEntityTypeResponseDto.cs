using TecFlow.Core.Entities;

namespace TecFlow.Business.Dto;

public class YourItemEntityTypeResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public YourItemEntityType? Data { get; set; }
    public List<YourItemEntityType>? DataList { get; set; }
}

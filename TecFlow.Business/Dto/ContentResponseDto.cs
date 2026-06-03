using TecFlow.Core.Entities;

namespace TecFlow.Business.Dto;

public class ContentResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public Content? Data { get; set; }
    public List<Content>? DataList { get; set; }
}

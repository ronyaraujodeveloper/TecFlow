using TecFlow.Core.Entities;

namespace TecFlow.Business.Dto;

public class ConversionResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public Conversion? Data { get; set; }
    public List<Conversion>? DataList { get; set; }
}

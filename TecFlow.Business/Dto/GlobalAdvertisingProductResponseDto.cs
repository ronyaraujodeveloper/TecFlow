using TecFlow.Core.Entities;

namespace TecFlow.Business.Dto;

public class GlobalAdvertisingProductResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public GlobalAdvertisingProduct? Data { get; set; }
    public List<GlobalAdvertisingProduct>? DataList { get; set; }
    public OptimizedPostPayloadDto? Payload { get; set; }
}

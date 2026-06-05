using TecFlow.Core.Entities;

namespace TecFlow.Business.Dto;

public class MetricResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public Metric? Data { get; set; }
    public List<Metric>? DataList { get; set; }
    public PagingInfoDto? Paging { get; set; }
}

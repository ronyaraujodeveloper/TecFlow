using TecFlow.Core.Entities;

namespace TecFlow.Business.Dto;

public class SalesOrderResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public SalesOrder? Data { get; set; }
    public List<SalesOrder>? DataList { get; set; }
    public PagingInfoDto? Paging { get; set; }
}

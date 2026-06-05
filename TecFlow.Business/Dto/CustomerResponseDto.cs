using TecFlow.Core.Entities;

namespace TecFlow.Business.Dto;

public class CustomerResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public Customer? Data { get; set; }
    public List<Customer>? DataList { get; set; }
    public PagingInfoDto? Paging { get; set; }
}

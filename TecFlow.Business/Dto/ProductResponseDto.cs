using TecFlow.Core.Entities;

namespace TecFlow.Business.Dto;

public class ProductResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public Product? Data { get; set; }
    public List<Product>? DataList { get; set; }
    public PagingInfoDto? Paging { get; set; }
}

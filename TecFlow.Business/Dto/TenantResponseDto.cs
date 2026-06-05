using TecFlow.Core.Entities;

namespace TecFlow.Business.Dto;

public class TenantResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public Tenant? Data { get; set; }
    public List<Tenant>? DataList { get; set; }
}

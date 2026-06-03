using TecFlow.Core.Entities;

namespace TecFlow.Business.Dto;

public class AffiliateResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public Affiliate? Data { get; set; }
    public List<Affiliate>? DataList { get; set; }
}

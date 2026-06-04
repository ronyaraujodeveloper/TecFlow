using TecFlow.Core.Entities;

namespace TecFlow.Business.Dto;

public class AffiliateLinkResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public AffiliateLink? Data { get; set; }
    public List<AffiliateLink>? DataList { get; set; }
}

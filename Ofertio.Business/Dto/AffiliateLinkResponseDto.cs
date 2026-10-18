using Ofertio.Core.Entities;

namespace Ofertio.Business.Dto;

public class AffiliateLinkResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public AffiliateLink? Data { get; set; }
    public List<AffiliateLink>? DataList { get; set; }
}

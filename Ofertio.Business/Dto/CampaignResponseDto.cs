using Ofertio.Core.Entities;

namespace Ofertio.Business.Dto;

public class CampaignResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public Campaign? Data { get; set; }
    public List<Campaign>? DataList { get; set; }
    public PagingInfoDto? Paging { get; set; }
}

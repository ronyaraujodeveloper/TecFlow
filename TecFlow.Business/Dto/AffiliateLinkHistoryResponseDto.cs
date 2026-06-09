namespace TecFlow.Business.Dto;

public class AffiliateLinkHistoryResponseDto
{
    public bool Status { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public List<AffiliateLinkHistoryItemDto>? DataList { get; set; }

    public PagingInfoDto? Paging { get; set; }
}

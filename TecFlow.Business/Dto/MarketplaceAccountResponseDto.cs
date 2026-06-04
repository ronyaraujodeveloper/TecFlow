using TecFlow.Core.Entities;

namespace TecFlow.Business.Dto;

public class MarketplaceAccountResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public MarketplaceAccount? Data { get; set; }
    public List<MarketplaceAccount>? DataList { get; set; }
}

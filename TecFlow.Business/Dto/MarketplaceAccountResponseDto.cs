namespace TecFlow.Business.Dto;

/// <summary>Envelope padronizado de conta marketplace (vinculação manual / OAuth).</summary>
public class MarketplaceAccountResponseDto
{
    public bool Status { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public MarketplaceAccountDto? Data { get; set; }

    public List<MarketplaceAccountDto>? DataList { get; set; }
}

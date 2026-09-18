namespace TecFlow.Business.Dto;

/// <summary>Envelope padronizado de conta marketplace (vinculação manual / OAuth).</summary>
public class MarketplaceAccountResponseDto : ResponseDto
{
    public MarketplaceAccountDto? Data { get; set; }

    public List<MarketplaceAccountDto>? DataList { get; set; }

    public static MarketplaceAccountResponseDto Ok(
        MarketplaceAccountDto? data = null,
        string descricao = "Loja vinculada com sucesso") =>
        new()
        {
            Status = true,
            Descricao = descricao,
            Data = data
        };

    public static new MarketplaceAccountResponseDto Fail(string descricao) =>
        new()
        {
            Status = false,
            Descricao = descricao
        };
}

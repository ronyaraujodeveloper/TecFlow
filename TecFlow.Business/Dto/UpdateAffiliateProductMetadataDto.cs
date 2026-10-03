namespace TecFlow.Business.Dto;

/// <summary>Nome, preço e imagem informados pelo afiliado quando o scrape falha ou precisa de ajuste.</summary>
public class UpdateAffiliateProductMetadataDto
{
    public string? ProductName { get; set; }

    public decimal? ProductPrice { get; set; }

    public string? ProductImageUrl { get; set; }
}

namespace TecFlow.Business.Dto;

/// <summary>Nome, preço e imagem extraídos da página do produto (OpenGraph / JSON-LD).</summary>
public sealed class ProductMetadataDto
{
    public string? ProductName { get; set; }

    public decimal? ProductPrice { get; set; }

    public string? ProductImageUrl { get; set; }
}

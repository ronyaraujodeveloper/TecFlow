namespace TecFlow.Business.Dto;

/// <summary>Nome e preço informados manualmente pelo afiliado quando o antibot bloqueia o scrape.</summary>
public class UpdateAffiliateProductMetadataDto
{
    public string? ProductName { get; set; }

    public decimal? ProductPrice { get; set; }
}

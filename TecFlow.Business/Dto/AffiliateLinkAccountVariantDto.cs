namespace TecFlow.Business.Dto;

/// <summary>Variação de URL convertida para uma conta/loja específica.</summary>
public class AffiliateLinkAccountVariantDto
{
    public Guid AffiliateLinkId { get; set; }

    public int StoreId { get; set; }

    public int? MarketplaceAccountId { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public string AffiliateUrl { get; set; } = string.Empty;

    public string ShortenedUrl { get; set; } = string.Empty;

    public string ShortenedShopeeUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

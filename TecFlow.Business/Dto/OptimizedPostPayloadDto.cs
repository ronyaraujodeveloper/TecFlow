using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

/// <summary>Metadados padronizados para postagem automática ou resposta em redes sociais.</summary>
public class OptimizedPostPayloadDto
{
    public Guid GlobalProductId { get; set; }
    public MarketplaceType Platform { get; set; }
    public string FriendlyName { get; set; } = string.Empty;
    public string FormattedPrice { get; set; } = string.Empty;
    public string? MainImageUrl { get; set; }
    public string AffiliateLink { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string GlobalCategory { get; set; } = string.Empty;
}

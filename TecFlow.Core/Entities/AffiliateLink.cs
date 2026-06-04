using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Enums;

namespace TecFlow.Core.Entities;

/// <summary>
/// Produto de divulgação com link original e variações rastreáveis por marketplace (Shopee / TikTok Shop).
/// </summary>
[Table("LinksAfiliado")]
public class AffiliateLink : BaseEntity
{
    [Column("ProdutoId")]
    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int OwnerId { get; set; }

    public UserAccount? Owner { get; set; }

    /// <summary>URL de destino antes da parametrização de afiliado.</summary>
    [Column("LinkOriginal")]
    [MaxLength(2048)]
    public string OriginalUrl { get; set; } = string.Empty;

    [Column("LinkShopee")]
    [MaxLength(2048)]
    public string? ShopeeTrackedUrl { get; set; }

    [Column("LinkTikTokShop")]
    [MaxLength(2048)]
    public string? TikTokShopTrackedUrl { get; set; }

    [Column("IdExternoShopee")]
    [MaxLength(128)]
    public string? ShopeeExternalProductId { get; set; }

    [Column("IdExternoTikTok")]
    [MaxLength(128)]
    public string? TikTokShopExternalProductId { get; set; }

    [Column("CodigoRastreio")]
    [MaxLength(64)]
    public string? TrackingCode { get; set; }

    [Column("RedeSocial")]
    public SocialMediaType? PrimarySocialMedia { get; set; }

    [Column("MarketplacePrincipal")]
    public MarketplaceType? PrimaryMarketplace { get; set; }

    [Column("Ativo")]
    public bool IsActive { get; set; } = true;

    [Column("ParametrosExtras")]
    public string? ExtraTrackingParameters { get; set; }
}

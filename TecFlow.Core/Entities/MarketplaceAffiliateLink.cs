using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;
using TecFlow.Core.Enums;

namespace TecFlow.Core.Entities;

/// <summary>Vínculo de afiliado por marketplace para um produto global de propaganda.</summary>
[Table("MarketplaceAffiliateLinks")]
public class MarketplaceAffiliateLink : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }

    [Column("ProdutoGlobalId")]
    public int GlobalProductId { get; set; }

    public GlobalAdvertisingProduct? GlobalProduct { get; set; }

    [Column("Marketplace")]
    public MarketplaceType MarketplaceType { get; set; }

    [Column("UrlProdutoOriginal")]
    [MaxLength(2048)]
    public string OriginalProductUrl { get; set; } = string.Empty;

    [Column("IdProdutoPlataforma")]
    [MaxLength(128)]
    public string? PlatformProductId { get; set; }

    [Column("LinkAfiliadoGerado")]
    [MaxLength(2048)]
    public string GeneratedAffiliateLink { get; set; } = string.Empty;

    [Column("ParametrosRastreio")]
    public string? CustomTrackingParameters { get; set; }
}

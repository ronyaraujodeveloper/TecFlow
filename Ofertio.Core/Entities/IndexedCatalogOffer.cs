using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ofertio.Core.Enums;

namespace Ofertio.Core.Entities;

/// <summary>Oferta gravada pela Busca de Ofertas para a consulta híbrida do radar e do comparador.</summary>
[Table("IndexedCatalogOffers")]
public class IndexedCatalogOffer : BaseEntity
{
    public int UserId { get; set; }

    [Required]
    [MaxLength(120)]
    public string SearchTerm { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? ProductModel { get; set; }

    [Required]
    [MaxLength(255)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ProductImageUrl { get; set; }

    [Required]
    [MaxLength(1000)]
    public string SourceUrl { get; set; } = string.Empty;

    public MarketplaceType? PlatformType { get; set; }

    [MaxLength(64)]
    public string? PlatformName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    [MaxLength(120)]
    public string? Shipping { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? CommissionPercent { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? EstimatedCommission { get; set; }

    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
}

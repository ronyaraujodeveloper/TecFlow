using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

/// <summary>Produto global de propaganda/divulgação (catálogo de afiliado, não estoque físico).</summary>
[Table("ProdutosPropagandaGlobal")]
public class GlobalAdvertisingProduct : BaseEntity
{
    public Guid GlobalProductUid { get; set; } = Guid.NewGuid();

    [Column("NomeAmigavel")]
    [MaxLength(256)]
    public string FriendlyName { get; set; } = string.Empty;

    [Column("Descricao")]
    public string Description { get; set; } = string.Empty;

    [Column("CategoriaGlobal")]
    [MaxLength(128)]
    public string GlobalCategory { get; set; } = string.Empty;

    [Column("UrlImagemPrincipal")]
    [MaxLength(2048)]
    public string? MainImageUrl { get; set; }

    [Column("PrecoMedio")]
    public decimal AveragePrice { get; set; }

    public int OwnerId { get; set; }

    public UserAccount? Owner { get; set; }

    public ICollection<MarketplaceAffiliateLink> MarketplaceLinks { get; set; } =
        new List<MarketplaceAffiliateLink>();
}

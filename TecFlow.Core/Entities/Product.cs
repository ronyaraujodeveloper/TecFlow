using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Enums;

namespace TecFlow.Core.Entities;

[Table("Produtos")]
public class Product : BaseEntity
{
    [Column("Nome")]
    public string Name { get; set; } = string.Empty;

    [Column("Resumo")]
    public string Summary { get; set; } = string.Empty;

    [Column("Descricao")]
    public string Description { get; set; } = string.Empty;

    [Column("Caracteristicas")]
    public string Features { get; set; } = string.Empty;

    [Column("Beneficios")]
    public string Benefits { get; set; } = string.Empty;

    [Column("Categoria")]
    public string Category { get; set; } = string.Empty;

    [Column("PublicoAlvo")]
    public string TargetAudience { get; set; } = string.Empty;

    [Column("Preco")]
    public decimal Price { get; set; }

    [Column("DataCriacao")]
    public DateTime CreatedOn { get; set; }

    [Column("DataModificacao")]
    public DateTime ModifiedOn { get; set; }

    [Column("UrlImagemPrincipal")]
    public string? MainImageUrl { get; set; }

    [Column("UrlsImagens")]
    public List<string> ImageUrls { get; set; } = new();

    public decimal SalesVolume { get; set; }
    public double Rating { get; set; }
    public string? Material { get; set; }

    [Column("Estoque")]
    public int Stock { get; set; }

    [Column("Cor")]
    public string? Color { get; set; }

    public int OwnerId { get; set; }
    public UserAccount? Owner { get; set; }

    [Column("IdExterno")]
    [MaxLength(128)]
    public string? ExternalProductId { get; set; }

    [Column("SkuCodigo")]
    [MaxLength(128)]
    public string? SkuCode { get; set; }

    [Column("MarketplaceOrigem")]
    public MarketplaceType? MarketplaceSource { get; set; }

    [Column("MarketplaceShopId")]
    [MaxLength(128)]
    public string? MarketplaceShopId { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

/// <summary>Característica de produto usada no filtro do feed de ofertas (N:N com <see cref="GroupCapturedMessage"/> via OfferTags).</summary>
[Table("ProductTags")]
public class ProductTag
{
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<GroupCapturedMessage> Offers { get; set; } = new List<GroupCapturedMessage>();
}

public static class ProductTagCatalog
{
    /// <summary>Opção do combo sem filtro; não é linha do banco para não exigir vínculo em toda oferta.</summary>
    public const string AllTagsLabel = "Todas as Características";

    public const string Gamer = "gamer";
    public const string Eletronicos = "eletronicos";
    public const string Informatica = "informatica";
    public const string CasaCozinha = "casa-cozinha";
    public const string Smartphones = "smartphones";
    public const string ModaBeleza = "moda-beleza";

    /// <summary>Ids fixos: são a chave do HasData e não podem mudar depois de migrados.</summary>
    public static readonly IReadOnlyList<(int Id, string Name, string Slug)> Seed =
    [
        (1, "Gamer", Gamer),
        (2, "Eletrônicos", Eletronicos),
        (3, "Informática", Informatica),
        (4, "Casa & Cozinha", CasaCozinha),
        (5, "Smartphones", Smartphones),
        (6, "Moda/Beleza", ModaBeleza)
    ];
}

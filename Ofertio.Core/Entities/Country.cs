using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ofertio.Core.Entities;

/// <summary>País (tabela de referência, carregada pela migration). Todos os países existem; estados e cidades, por enquanto, só do Brasil.</summary>
[Table("Countries")]
public class Country
{
    public const string BrazilIsoCode = "BR";

    public int Id { get; set; }

    /// <summary>Código ISO 3166-1 alfa-2, por exemplo BR.</summary>
    [Required]
    [MaxLength(2)]
    public string IsoCode { get; set; } = string.Empty;

    /// <summary>Nome em português.</summary>
    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;
}

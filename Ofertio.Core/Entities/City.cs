using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ofertio.Core.Entities;

/// <summary>Município, tabela de referência carregada pela migration a partir da lista do IBGE.</summary>
[Table("Cities")]
public class City
{
    public int Id { get; set; }

    public int StateId { get; set; }

    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Nome sem acento e minúsculo, usado para casar o texto que vem da localização por IP.</summary>
    [Required]
    [MaxLength(80)]
    public string NormalizedName { get; set; } = string.Empty;

    /// <summary>Código do IBGE (7 dígitos), o mesmo que o ViaCEP devolve.</summary>
    public int IbgeCode { get; set; }

    public State? State { get; set; }
}

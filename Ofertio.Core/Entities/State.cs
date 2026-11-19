using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ofertio.Core.Entities;

/// <summary>Estado (UF), tabela de referência carregada pela migration a partir da lista do IBGE.</summary>
[Table("States")]
public class State
{
    public int Id { get; set; }

    public int CountryId { get; set; }

    /// <summary>Sigla, por exemplo SP.</summary>
    [Required]
    [MaxLength(2)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Código do IBGE (2 dígitos).</summary>
    public int IbgeCode { get; set; }

    public Country? Country { get; set; }
}

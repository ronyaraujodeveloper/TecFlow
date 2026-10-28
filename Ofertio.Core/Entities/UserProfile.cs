using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ofertio.Core.Entities;

/// <summary>Dados cadastrais do cliente (pessoa física ou empresa), exigidos para assinar um plano.</summary>
[Table("PerfisCadastrais")]
public class UserProfile : BaseEntity
{
    public int UserId { get; set; }

    [MaxLength(2)]
    public string PersonType { get; set; } = PersonTypes.Individual;

    /// <summary>Nome completo (pessoa física) ou razão social (empresa).</summary>
    [MaxLength(160)]
    public string? FullName { get; set; }

    /// <summary>Responsável pela empresa; vazio para pessoa física.</summary>
    [MaxLength(160)]
    public string? ResponsibleName { get; set; }

    /// <summary>CPF ou CNPJ só com dígitos/letras; criptografado em repouso (LGPD).</summary>
    public string? Document { get; set; }

    [MaxLength(11)]
    public string? Phone { get; set; }

    [MaxLength(8)]
    public string? PostalCode { get; set; }

    [MaxLength(160)]
    public string? Street { get; set; }

    [MaxLength(16)]
    public string? Number { get; set; }

    [MaxLength(80)]
    public string? Complement { get; set; }

    [MaxLength(80)]
    public string? District { get; set; }

    [MaxLength(80)]
    public string? City { get; set; }

    [MaxLength(2)]
    public string? State { get; set; }
}

public static class PersonTypes
{
    public const string Individual = "PF";
    public const string Company = "PJ";
}

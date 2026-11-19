using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ofertio.Core.Abstractions;

namespace Ofertio.Core.Entities;

/// <summary>
/// Evento da página pública do afiliado, só para contagem: nada identifica o visitante.
/// Guarda o IP mascarado (último trecho oculto) apenas para estimar visitantes únicos, com retenção curta,
/// e a região estimada (país, estado, cidade). O IP completo nunca é gravado.
/// </summary>
[Table("PublicPageEvents")]
public class PublicPageEvent : BaseEntity, ITenantScopedEntity
{
    public const string KindView = "View";
    public const string KindConvert = "Convert";
    public const string KindJoin = "Join";

    public Guid TenantId { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(12)]
    public string Kind { get; set; } = KindView;

    /// <summary>Preenchido nos eventos de entrada em grupo.</summary>
    public int? JoinLinkId { get; set; }

    [MaxLength(16)]
    public string? Channel { get; set; }

    public bool IsBot { get; set; }

    [MaxLength(64)]
    public string? IpMasked { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    [MaxLength(32)]
    public string? DeviceType { get; set; }

    /// <summary>Região estimada pelo IP. Fora do Brasil, só o país.</summary>
    public int? CountryId { get; set; }

    public int? StateId { get; set; }

    public int? CityId { get; set; }

    /// <summary>Histórico: região em texto dos eventos gravados antes das tabelas de país, estado e cidade. Eventos novos usam só os IDs.</summary>
    [MaxLength(64)]
    public string? Country { get; set; }

    [MaxLength(64)]
    public string? Region { get; set; }

    [MaxLength(96)]
    public string? City { get; set; }
}

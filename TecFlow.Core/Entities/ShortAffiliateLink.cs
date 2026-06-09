using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;
using TecFlow.Core.Enums;

namespace TecFlow.Core.Entities;

/// <summary>Encurtador interno TecFlow mapeando código curto para URL de afiliado oficial.</summary>
[Table("ShortAffiliateLinks")]
public class ShortAffiliateLink : BaseEntity, ITenantScopedEntity
{
    /// <summary>Identificador público UUID do link gerado (telemetria).</summary>
    public Guid AffiliateLinkId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(8)]
    public string ShortCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(2048)]
    public string DestinationUrl { get; set; } = string.Empty;

    [Required]
    [MaxLength(2048)]
    public string OriginalUrl { get; set; } = string.Empty;

    public MarketplaceType PlatformType { get; set; }

    public int UserId { get; set; }

    public int? IntegracaoLojaId { get; set; }

    public Guid TenantId { get; set; }

    [MaxLength(256)]
    public string? CustomNickname { get; set; }

    public bool IsActive { get; set; } = true;
}

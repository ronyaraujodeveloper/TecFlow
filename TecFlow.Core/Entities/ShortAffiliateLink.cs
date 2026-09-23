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

    /// <summary>FK da conta em MarketplaceAccounts (SQL Server).</summary>
    public int? MarketplaceAccountId { get; set; }

    public Guid TenantId { get; set; }

    /// <summary>URL de afiliado gerada (Universal Link / tag de comissão).</summary>
    [Required]
    [MaxLength(2048)]
    public string AffiliateUrl { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? CustomNickname { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Alias de <see cref="ShortCode"/> para o contrato mobile/offline.</summary>
    [NotMapped]
    public string Code
    {
        get => ShortCode;
        set => ShortCode = value;
    }

    /// <summary>Alias de <see cref="PlatformType"/>.</summary>
    [NotMapped]
    public MarketplaceType Platform
    {
        get => PlatformType;
        set => PlatformType = value;
    }
}

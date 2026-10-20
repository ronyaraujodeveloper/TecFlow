using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ofertio.Core.Entities;

[Table("AffiliateMiningProfiles")]
public class AffiliateMiningProfile : BaseEntity
{
    public int UserId { get; set; }

    [MaxLength(1000)]
    public string NichesCsv { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinTicket { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxTicket { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal MinCommissionPercent { get; set; } = 5;

    /// <summary>Taxa de comissão de afiliado do usuário no Mercado Livre (o ML não expõe a taxa por API).</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MercadoLivreCommissionPercent { get; set; }

    /// <summary>Taxa de comissão de afiliado do usuário na Amazon (a PA-API não expõe a taxa).</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? AmazonCommissionPercent { get; set; }

    public bool RestrictToActiveStores { get; set; } = true;

    public bool AutoPilotEnabled { get; set; }

    [MaxLength(16)]
    public string AutoPilotChannel { get; set; } = "WhatsApp";
}

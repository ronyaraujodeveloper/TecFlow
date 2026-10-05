using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

[Table("OfferHealthAlerts")]
public class OfferHealthAlert : BaseEntity
{
    public int UserId { get; set; }

    [MaxLength(16)]
    public string Channel { get; set; } = "WhatsApp";

    public int? CampaignId { get; set; }

    [MaxLength(1000)]
    public string? ProductUrl { get; set; }

    [MaxLength(32)]
    public string? CouponCode { get; set; }

    [Required]
    [MaxLength(32)]
    public string AlertType { get; set; } = OfferHealthAlertTypes.Esgotado;

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ComparedPrice { get; set; }

    public bool IsRead { get; set; }
}

public static class OfferHealthAlertTypes
{
    public const string Esgotado = "Esgotado";
    public const string PrecoAlterado = "PrecoAlterado";
    public const string CupomInvalido = "CupomInvalido";
}

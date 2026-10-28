using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ofertio.Core.Entities;

/// <summary>
/// Pedido de assinatura feito em Meu plano. O plano do inquilino só muda quando o gateway confirma o pagamento;
/// do cartão ficam apenas bandeira e últimos 4 dígitos.
/// </summary>
[Table("PedidosAssinatura")]
public class PlanCheckout : BaseEntity
{
    public Guid TenantId { get; set; }

    public int UserId { get; set; }

    [MaxLength(32)]
    public string PlanCode { get; set; } = string.Empty;

    [MaxLength(64)]
    public string PlanName { get; set; } = string.Empty;

    /// <summary>Mensal ou Anual.</summary>
    [MaxLength(16)]
    public string BillingCycle { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    [MaxLength(24)]
    public string Status { get; set; } = PlanCheckoutStatuses.Pending;

    [MaxLength(24)]
    public string? CardBrand { get; set; }

    [MaxLength(4)]
    public string? CardLast4 { get; set; }

    [MaxLength(64)]
    public string? GatewayPaymentId { get; set; }

    [MaxLength(512)]
    public string? Message { get; set; }
}

public static class PlanCheckoutStatuses
{
    public const string Pending = "Pendente";
    public const string Simulated = "Simulado";
    public const string Paid = "Pago";
    public const string Declined = "Recusado";
}

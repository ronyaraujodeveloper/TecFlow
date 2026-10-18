namespace Ofertio.Core.Entities;

/// <summary>Pagamento recebido do gateway (ASAAS) para faturamento do SaaS.</summary>
public class SaasPayment : BaseEntity
{
    public Guid TenantId { get; set; }

    public string GatewayPaymentId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal NetAmount { get; set; }

    /// <summary>Status do gateway (RECEIVED, CONFIRMED, OVERDUE, REFUNDED...).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>PIX, BOLETO, CREDIT_CARD ou UNDEFINED.</summary>
    public string BillingType { get; set; } = string.Empty;

    public DateTime? DueDate { get; set; }

    public DateTime? PaidAt { get; set; }
}

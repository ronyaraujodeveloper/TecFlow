namespace Ofertio.Business.Dto;

/// <summary>Pedido de assinatura de Meu plano.</summary>
public class PlanCheckoutDto
{
    public int Id { get; set; }

    public string PlanCode { get; set; } = string.Empty;

    public string? PlanName { get; set; }

    /// <summary>Mensal ou Anual.</summary>
    public string BillingCycle { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Status { get; set; }

    public string? CardBrand { get; set; }

    public string? CardLast4 { get; set; }

    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Só na escrita: repassado ao gateway e descartado; nunca é gravado, logado nem devolvido.</summary>
    public CreditCardDto? Card { get; set; }
}

public class CreditCardDto
{
    public string Number { get; set; } = string.Empty;

    public string HolderName { get; set; } = string.Empty;

    /// <summary>Validade no formato MM/AA ou MM/AAAA.</summary>
    public string Expiry { get; set; } = string.Empty;

    public string Cvv { get; set; } = string.Empty;
}

public class PlanCheckoutResponseDto : ResponseDto
{
    public PlanCheckoutDto? Data { get; set; }

    /// <summary>Preenchido quando o pedido foi recusado por falta de dados cadastrais.</summary>
    public List<string> MissingFields { get; set; } = [];
}

/// <summary>Resumo da tela Meu plano: assinatura atual, planos à venda e pendências do cadastro.</summary>
public class MyPlanResponseDto : ResponseDto
{
    public bool IsMaster { get; set; }

    /// <summary>Conta Master liberada por configuração para testar o fluxo como cliente (IsMaster fica false).</summary>
    public bool IsMasterCheckoutTest { get; set; }

    public string? CurrentPlanCode { get; set; }

    public string? CurrentPlanName { get; set; }

    public string? BillingCycle { get; set; }

    public string? TenantStatus { get; set; }

    public DateTime? TrialEndsAt { get; set; }

    public DateTime? RenewalAt { get; set; }

    /// <summary>Verdadeiro enquanto o pagamento é simulado (nenhuma cobrança real).</summary>
    public bool IsPaymentSimulated { get; set; }

    public List<string> MissingFields { get; set; } = [];

    public List<SaasPlanDto> Plans { get; set; } = [];

    public PlanCheckoutDto? LastCheckout { get; set; }
}

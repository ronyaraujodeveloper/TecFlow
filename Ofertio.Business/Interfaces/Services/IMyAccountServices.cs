using Ofertio.Business.Dto;

namespace Ofertio.Business.Interfaces.Services;

/// <summary>Dados cadastrais (Meus dados) do usuário autenticado.</summary>
public interface IUserProfileService
{
    Task<UserProfileResponseDto> GetAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserProfileResponseDto> SaveAsync(int userId, UserProfileDto request, CancellationToken cancellationToken = default);
}

/// <summary>Tela Meu plano: assinatura atual, planos à venda e pedido de assinatura.</summary>
public interface IMyPlanService
{
    Task<MyPlanResponseDto> GetOverviewAsync(int userId, CancellationToken cancellationToken = default);

    Task<PlanCheckoutResponseDto> CheckoutAsync(int userId, PlanCheckoutDto request, CancellationToken cancellationToken = default);

    Task<PlanCheckoutResponseDto> GetCheckoutAsync(int userId, int checkoutId, CancellationToken cancellationToken = default);
}

/// <summary>Cobrança do plano escolhido. A fase atual usa a versão simulada; a cobrança real (ASAAS) entra depois.</summary>
public interface IPlanPaymentGateway
{
    bool IsSimulated { get; }

    Task<PlanPaymentResult> ChargeAsync(PlanPaymentRequest request, CancellationToken cancellationToken = default);
}

public sealed record PlanPaymentRequest(
    Guid TenantId,
    int UserId,
    string PlanCode,
    string BillingCycle,
    decimal Amount,
    UserProfileDto Payer,
    CreditCardDto Card);

public sealed record PlanPaymentResult(bool Approved, string Status, string Message, string? GatewayPaymentId);

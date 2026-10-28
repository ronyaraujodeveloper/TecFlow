using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Entities;

namespace Ofertio.Infrastructure.Services.Billing;

/// <summary>Gateway da fase de telas: aprova sem cobrar e sem enviar o cartão a lugar nenhum.</summary>
public sealed class SimulatedPlanPaymentGateway : IPlanPaymentGateway
{
    public const string SimulatedMessage =
        "Pagamento simulado: nenhuma cobrança foi feita e o seu plano não mudou. A cobrança real entra na próxima fase.";

    public bool IsSimulated => true;

    public Task<PlanPaymentResult> ChargeAsync(PlanPaymentRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PlanPaymentResult(true, PlanCheckoutStatuses.Simulated, SimulatedMessage, null));
}

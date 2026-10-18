using Ofertio.Business.Domain.Commission;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Interfaces.Orchestration;

/// <summary>
/// Concilia comissões reportadas pelos marketplaces com registos locais (pedidos, conversões, links).
/// Implementação alvo: Ofertio.Orquestrador e painel WebUi (Fase 6.2).
/// </summary>
public interface ICommissionConciliator
{
    Task<CommissionConciliationResult> ReconcileOwnerAsync(
        int ownerId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken = default);

    Task<CommissionConciliationResult> ReconcileOrderAsync(
        string externalOrderId,
        MarketplaceType marketplace,
        CancellationToken cancellationToken = default);
}

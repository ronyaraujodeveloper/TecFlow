using Ofertio.Business.Domain.Engagement;

namespace Ofertio.Business.Interfaces.Orchestration;

/// <summary>
/// Orquestra triagem de comentários/mensagens e decisão de envio automático de links de afiliado.
/// Implementação alvo: Ofertio.Orquestrador (+ filas na Fase 6).
/// </summary>
public interface IEngagementOrchestrator
{
    Task<EngagementOrchestrationResult> ProcessNewCommentAsync(
        SocialEngagementEvent engagementEvent,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EngagementOrchestrationResult>> ProcessPendingEngagementsAsync(
        int ownerId,
        CancellationToken cancellationToken = default);
}

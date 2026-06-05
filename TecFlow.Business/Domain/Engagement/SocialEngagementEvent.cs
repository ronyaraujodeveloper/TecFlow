using TecFlow.Core.Enums;

namespace TecFlow.Business.Domain.Engagement;

/// <summary>Evento de engajamento recebido via webhook, worker ou polling de rede social/marketplace.</summary>
public class SocialEngagementEvent
{
    public string ExternalEventId { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public SocialMediaType SocialMedia { get; set; }
    public string ContentExternalId { get; set; } = string.Empty;
    public string AuthorHandle { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public EngagementStatus Status { get; set; } = EngagementStatus.Pendente;
    public int? ProductId { get; set; }
    public int? AffiliateLinkId { get; set; }
}

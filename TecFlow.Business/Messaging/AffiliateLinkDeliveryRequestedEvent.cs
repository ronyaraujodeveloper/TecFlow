using TecFlow.Core.Enums;

namespace TecFlow.Business.Messaging;

/// <summary>Evento interno disparado após triagem positiva — indica entrega automatizada do link de afiliado.</summary>
public class AffiliateLinkDeliveryRequestedEvent
{
    public Guid CommentEventId { get; set; }
    public string PostId { get; set; } = string.Empty;
    public SocialMediaType Platform { get; set; }
    public string Username { get; set; } = string.Empty;
    public string MatchedKeyword { get; set; } = string.Empty;
    public string SimulatedAffiliateUrl { get; set; } = string.Empty;
    public DateTimeOffset RequestedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

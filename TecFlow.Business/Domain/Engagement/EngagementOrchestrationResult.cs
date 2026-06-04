using TecFlow.Core.Enums;

namespace TecFlow.Business.Domain.Engagement;

public class EngagementOrchestrationResult
{
    public bool Success { get; set; }
    public EngagementStatus FinalStatus { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AffiliateUrlSent { get; set; }
    public int? AffiliateLinkId { get; set; }
    public bool KeywordMatched { get; set; }
}

namespace TecFlow.Business.Interfaces.Messaging;

public interface ICommentKeywordTriageService
{
    bool IsEligibleForAffiliateLink(string commentText, out string? matchedKeyword);
}

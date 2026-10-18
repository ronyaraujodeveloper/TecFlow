using Microsoft.Extensions.Options;
using Ofertio.Business.Interfaces.Messaging;
using Ofertio.Business.Messaging;

namespace Ofertio.Infrastructure.Services.Messaging;

public class CommentKeywordTriageService : ICommentKeywordTriageService
{
    private readonly EngagementKeywordTriageOptions _options;

    public CommentKeywordTriageService(IOptions<EngagementKeywordTriageOptions> options)
    {
        _options = options.Value;
    }

    public bool IsEligibleForAffiliateLink(string commentText, out string? matchedKeyword)
    {
        matchedKeyword = null;
        if (string.IsNullOrWhiteSpace(commentText))
        {
            return false;
        }

        var normalized = commentText.Trim().ToLowerInvariant();
        foreach (var keyword in _options.Keywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (normalized.Contains(keyword.Trim().ToLowerInvariant(), StringComparison.Ordinal))
            {
                matchedKeyword = keyword;
                return true;
            }
        }

        return false;
    }
}

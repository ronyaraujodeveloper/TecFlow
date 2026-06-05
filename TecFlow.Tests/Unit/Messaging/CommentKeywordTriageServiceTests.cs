using Microsoft.Extensions.Options;
using TecFlow.Business.Messaging;
using TecFlow.Infrastructure.Services.Messaging;

namespace TecFlow.Tests.Unit.Messaging;

public class CommentKeywordTriageServiceTests
{
    [Theory]
    [InlineData("Eu quero o produto!", "eu quero")]
    [InlineData("manda o LINK por favor", "link")]
    [InlineData("quero comprar", "quero")]
    [InlineData("me manda no direct", "me manda")]
    public void IsEligibleForAffiliateLink_ShouldReturnTrue_WhenKeywordMatches(
        string comment,
        string expectedKeyword)
    {
        var service = CreateService();
        var eligible = service.IsEligibleForAffiliateLink(comment, out var matched);
        Assert.True(eligible);
        Assert.Equal(expectedKeyword, matched);
    }

    [Fact]
    public void IsEligibleForAffiliateLink_ShouldReturnFalse_WhenNoKeywordMatches()
    {
        var service = CreateService();
        var eligible = service.IsEligibleForAffiliateLink("obrigado pelo vídeo", out var matched);
        Assert.False(eligible);
        Assert.Null(matched);
    }

    private static CommentKeywordTriageService CreateService() =>
        new(Options.Create(new EngagementKeywordTriageOptions()));
}

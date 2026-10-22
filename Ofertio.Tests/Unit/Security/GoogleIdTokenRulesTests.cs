using Ofertio.Business.Service.Security;

namespace Ofertio.Tests.Unit.Security;

public class GoogleIdTokenRulesTests
{
    private const string ClientId = "ofertio-test-client";

    [Theory]
    [InlineData("ofertio-test-client", true)]
    [InlineData(" outro-client ", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void AudienceMatches_ShouldAcceptOnlyTheApplicationClientId(string? audience, bool expected)
    {
        Assert.Equal(expected, GoogleIdTokenRules.AudienceMatches(audience, ClientId));
    }

    [Fact]
    public void AudienceMatches_ShouldRejectWhenTheApplicationClientIdIsMissing()
    {
        Assert.False(GoogleIdTokenRules.AudienceMatches(ClientId, "  "));
        Assert.False(GoogleIdTokenRules.AudienceMatches(ClientId, null));
    }
}

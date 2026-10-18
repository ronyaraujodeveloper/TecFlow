using Ofertio.Business.Integrations.Auth;

namespace Ofertio.Tests.Unit.Integrations;

public class OAuthRedirectUriNormalizerTests
{
    private const string Clean = "https://camcorder-bonding-sloppily.ngrok-free.dev/integracoes/oauth/callback";

    [Theory]
    [InlineData("https://camcorder-bonding-sloppily.ngrok-free.dev:7002/integracoes/oauth/callback")]
    [InlineData("http://camcorder-bonding-sloppily.ngrok-free.dev:5000/integracoes/oauth/callback")]
    [InlineData("https://Camcorder-Bonding-Sloppily.NGROK-FREE.dev/integracoes/oauth/callback/")]
    [InlineData("https://camcorder-bonding-sloppily.ngrok-free.dev:7002/integracoes/oauth/callback?x=1#frag")]
    [InlineData("  https://camcorder-bonding-sloppily.ngrok-free.dev:7002  ")]
    [InlineData(Clean)]
    public void Normalize_ShouldProduceCleanNgrokCallback(string input)
    {
        Assert.Equal(Clean, OAuthRedirectUriNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("https://abc.ngrok.io:443/integracoes/oauth/callback", "https://abc.ngrok.io/integracoes/oauth/callback")]
    [InlineData("https://abc.ngrok.app:7002/integracoes/oauth/callback", "https://abc.ngrok.app/integracoes/oauth/callback")]
    public void Normalize_ShouldCoverOtherNgrokDomains(string input, string expected)
    {
        Assert.Equal(expected, OAuthRedirectUriNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("https://localhost:7002/integracoes/oauth/callback")]
    [InlineData("http://localhost:5000/integracoes/oauth/callback")]
    [InlineData("https://app.ofertio.com.br:8443/integracoes/oauth/callback")]
    [InlineData("")]
    public void Normalize_ShouldKeepNonTunnelUrisUntouched(string input)
    {
        Assert.Equal(input, OAuthRedirectUriNormalizer.Normalize(input));
    }
}

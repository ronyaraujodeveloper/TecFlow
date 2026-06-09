using Microsoft.Extensions.Options;
using TecFlow.SharedUi.Configuration;
using TecFlow.SharedUi.Models.Enums;

namespace TecFlow.SharedUi.Services.Auth;

public class OAuthProviderRegistry : IOAuthProviderRegistry
{
    private readonly PortalAuthenticationOptions _options;

    public OAuthProviderRegistry(IOptions<PortalAuthenticationOptions> options)
    {
        _options = options.Value;
    }

    public bool IsEnabled(AuthProvider provider) => provider switch
    {
        AuthProvider.Facebook => _options.Facebook.Enabled
            && !string.IsNullOrWhiteSpace(_options.Facebook.ClientId)
            && !string.IsNullOrWhiteSpace(_options.Facebook.ClientSecret),
        AuthProvider.Google => _options.Google.Enabled
            && !string.IsNullOrWhiteSpace(_options.Google.ClientId)
            && !string.IsNullOrWhiteSpace(_options.Google.ClientSecret),
        AuthProvider.ICloud => _options.Apple.Enabled
            && !string.IsNullOrWhiteSpace(_options.Apple.ClientId)
            && !string.IsNullOrWhiteSpace(_options.Apple.KeyId)
            && !string.IsNullOrWhiteSpace(_options.Apple.TeamId)
            && !string.IsNullOrWhiteSpace(_options.Apple.PrivateKey),
        AuthProvider.EmailPassword => true,
        _ => false
    };

    public string GetChallengePath(AuthProvider provider, LoginPlatform platform)
    {
        var providerKey = provider switch
        {
            AuthProvider.Google => "google",
            AuthProvider.Facebook => "facebook",
            AuthProvider.ICloud => "apple",
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };

        return $"/auth/challenge/{providerKey}?platform={platform}";
    }

    public string GetLinkChallengePath(AuthProvider provider, LoginPlatform platform)
    {
        var providerKey = provider switch
        {
            AuthProvider.Google => "google",
            AuthProvider.Facebook => "facebook",
            AuthProvider.ICloud => "apple",
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };

        return $"/auth/challenge/{providerKey}?platform={platform}&link=true";
    }
}

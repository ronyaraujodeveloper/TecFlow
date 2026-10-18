using Ofertio.SharedUi.Models.Enums;

namespace Ofertio.SharedUi.Services.Auth;

public interface IOAuthProviderRegistry
{
    bool IsEnabled(AuthProvider provider);
    string GetChallengePath(AuthProvider provider, LoginPlatform platform);

    string GetLinkChallengePath(AuthProvider provider, LoginPlatform platform);
}

using TecFlow.Portal.Models.Enums;

namespace TecFlow.Portal.Services.Auth;

public interface IOAuthProviderRegistry
{
    bool IsEnabled(AuthProvider provider);
    string GetChallengePath(AuthProvider provider, LoginPlatform platform);
}

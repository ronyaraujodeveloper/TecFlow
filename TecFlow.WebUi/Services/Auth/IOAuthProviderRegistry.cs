using TecFlow.WebUi.Models.Enums;

namespace TecFlow.WebUi.Services.Auth;

public interface IOAuthProviderRegistry
{
    bool IsEnabled(AuthProvider provider);
    string GetChallengePath(AuthProvider provider, LoginPlatform platform);
}

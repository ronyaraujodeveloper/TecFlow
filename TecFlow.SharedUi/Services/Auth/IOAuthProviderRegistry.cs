using TecFlow.SharedUi.Models.Enums;

namespace TecFlow.SharedUi.Services.Auth;

public interface IOAuthProviderRegistry
{
    bool IsEnabled(AuthProvider provider);
    string GetChallengePath(AuthProvider provider, LoginPlatform platform);
}

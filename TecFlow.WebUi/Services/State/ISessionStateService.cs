using TecFlow.WebUi.Models.Enums;

namespace TecFlow.WebUi.Services.State;

public interface ISessionStateService
{
    string? AccessToken { get; }
    string? RefreshToken { get; }
    string? UserId { get; }
    string? DisplayName { get; }
    LoginPlatform? ActivePlatform { get; }
    bool IsAuthenticated { get; }
    event Action? OnChange;

    void SetSession(
        string accessToken,
        LoginPlatform platform,
        string? refreshToken = null,
        string? userId = null,
        string? displayName = null);

    void Clear();
}

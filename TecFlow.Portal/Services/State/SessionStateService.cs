using TecFlow.Portal.Models.Enums;

namespace TecFlow.Portal.Services.State;

public class SessionStateService : ISessionStateService
{
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? UserId { get; private set; }
    public string? DisplayName { get; private set; }
    public LoginPlatform? ActivePlatform { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

    public event Action? OnChange;

    public void SetSession(
        string accessToken,
        LoginPlatform platform,
        string? refreshToken = null,
        string? userId = null,
        string? displayName = null)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        UserId = userId;
        DisplayName = displayName;
        ActivePlatform = platform;
        OnChange?.Invoke();
    }

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        UserId = null;
        DisplayName = null;
        ActivePlatform = null;
        OnChange?.Invoke();
    }
}

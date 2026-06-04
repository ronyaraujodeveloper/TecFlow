using System.Security.Claims;
using TecFlow.SharedUi.Models.Enums;
using TecFlow.SharedUi.Models.Responses;
using TecFlow.SharedUi.Security;
using TecFlow.SharedUi.Services.Auth;
using TecFlow.SharedUi.Services.State;

namespace TecFlow.Mobile.Services;

/// <summary>Autenticação baseada em sessão em memória (sem cookies HTTP) para o shell MAUI.</summary>
public class SessionAuthCookieService : IAuthCookieService
{
    private readonly ISessionStateService _sessionState;

    public SessionAuthCookieService(ISessionStateService sessionState)
    {
        _sessionState = sessionState;
    }

    public Task SignInFromAuthResponseAsync(
        AuthTokenResponse response,
        LoginPlatform platform,
        AuthProvider provider,
        CancellationToken cancellationToken = default)
    {
        _sessionState.SetSession(
            response.Token,
            platform,
            response.RefreshToken,
            response.UserId,
            response.DisplayName);

        return Task.CompletedTask;
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        _sessionState.Clear();
        return Task.CompletedTask;
    }

    public void SyncSessionFromPrincipal(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var token = principal.FindFirst(TecFlowClaimTypes.AccessToken)?.Value;
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        if (!Enum.TryParse<LoginPlatform>(principal.FindFirst(TecFlowClaimTypes.Platform)?.Value, ignoreCase: true, out var platform))
        {
            return;
        }

        _sessionState.SetSession(
            token,
            platform,
            userId: principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            displayName: principal.FindFirst(ClaimTypes.Name)?.Value);
    }
}

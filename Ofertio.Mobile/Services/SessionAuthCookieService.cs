using System.Security.Claims;
using Ofertio.SharedUi.Models.Enums;
using Ofertio.SharedUi.Models.Responses;
using Ofertio.Core.Security;
using Ofertio.SharedUi.Services.Auth;
using Ofertio.SharedUi.Services.State;

namespace Ofertio.Mobile.Services;

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

        var token = principal.FindFirst(OfertioClaimTypes.AccessToken)?.Value;
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        if (!Enum.TryParse<LoginPlatform>(principal.FindFirst(OfertioClaimTypes.Platform)?.Value, ignoreCase: true, out var platform))
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

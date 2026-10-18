using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Ofertio.Core.Security;
using Ofertio.SharedUi.Services.State;

namespace Ofertio.Mobile.Services;

public class MobileAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ISessionStateService _sessionState;

    public MobileAuthenticationStateProvider(ISessionStateService sessionState)
    {
        _sessionState = sessionState;
        _sessionState.OnChange += () => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_sessionState.IsAuthenticated)
        {
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _sessionState.UserId ?? "0"),
            new(ClaimTypes.Name, _sessionState.DisplayName ?? "Utilizador"),
            new(OfertioClaimTypes.AccessToken, _sessionState.AccessToken!),
            new(OfertioClaimTypes.Platform, _sessionState.ActivePlatform?.ToString() ?? string.Empty)
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "OfertioMobile");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    public void NotifyStateChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}

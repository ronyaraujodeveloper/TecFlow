using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TecFlow.WebUi.Models.Enums;
using TecFlow.WebUi.Models.Responses;
using TecFlow.WebUi.Security;
using TecFlow.WebUi.Services.State;

namespace TecFlow.WebUi.Services.Auth;

public class AuthCookieService : IAuthCookieService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISessionStateService _sessionState;

    public AuthCookieService(IHttpContextAccessor httpContextAccessor, ISessionStateService sessionState)
    {
        _httpContextAccessor = httpContextAccessor;
        _sessionState = sessionState;
    }

    public async Task SignInFromAuthResponseAsync(
        AuthTokenResponse response,
        LoginPlatform platform,
        AuthProvider provider,
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext indisponível.");

        var principal = BuildPrincipal(response, platform, provider);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = response.ExpiresAt?.UtcDateTime ?? DateTimeOffset.UtcNow.AddHours(8).UtcDateTime
        };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);

        _sessionState.SetSession(
            response.Token,
            platform,
            response.RefreshToken,
            response.UserId,
            response.DisplayName);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        _sessionState.Clear();
    }

    public void SyncSessionFromPrincipal(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var token = principal.FindFirstValue(TecFlowClaimTypes.AccessToken);
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var platformValue = principal.FindFirstValue(TecFlowClaimTypes.Platform);
        if (!Enum.TryParse<LoginPlatform>(platformValue, ignoreCase: true, out var platform))
        {
            return;
        }

        _sessionState.SetSession(
            token,
            platform,
            userId: principal.FindFirstValue(ClaimTypes.NameIdentifier),
            displayName: principal.FindFirstValue(ClaimTypes.Name));
    }

    private static ClaimsPrincipal BuildPrincipal(
        AuthTokenResponse response,
        LoginPlatform platform,
        AuthProvider provider)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, response.UserId ?? "0"),
            new(ClaimTypes.Name, response.DisplayName ?? "Utilizador"),
            new(TecFlowClaimTypes.AccessToken, response.Token),
            new(TecFlowClaimTypes.Platform, platform.ToString()),
            new(TecFlowClaimTypes.AuthProvider, provider.ToString())
        };

        if (!string.IsNullOrEmpty(response.RefreshToken))
        {
            claims.Add(new Claim("TecFlow:refresh_token", response.RefreshToken));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}

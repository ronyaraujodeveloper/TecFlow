using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Ofertio.SharedUi.Models.Enums;
using Ofertio.SharedUi.Models.Responses;
using Ofertio.Core.Security;
using Ofertio.SharedUi.Services.Auth;
using Ofertio.SharedUi.Services.State;

namespace Ofertio.WebUi.Services.Auth;

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

        var token = principal.FindFirstValue(OfertioClaimTypes.AccessToken);
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var platformValue = principal.FindFirstValue(OfertioClaimTypes.Platform);
        if (!Enum.TryParse<LoginPlatform>(platformValue, ignoreCase: true, out var platform))
        {
            return;
        }

        _sessionState.SetSession(
            token,
            platform,
            refreshToken: principal.FindFirstValue("Ofertio:refresh_token"),
            userId: principal.FindFirstValue(ClaimTypes.NameIdentifier),
            displayName: principal.FindFirstValue(ClaimTypes.Name));
    }

    private static ClaimsPrincipal BuildPrincipal(
        AuthTokenResponse response,
        LoginPlatform platform,
        AuthProvider provider)
    {
        if (string.IsNullOrWhiteSpace(response.Token))
        {
            throw new InvalidOperationException("Token de autenticação ausente na resposta da API.");
        }

        var userId = string.IsNullOrWhiteSpace(response.UserId) ? "0" : response.UserId.Trim();
        var displayName = string.IsNullOrWhiteSpace(response.DisplayName)
            ? "Utilizador Ofertio"
            : response.DisplayName.Trim();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, displayName),
            new(OfertioClaimTypes.AccessToken, response.Token),
            new(OfertioClaimTypes.Platform, platform.ToString()),
            new(OfertioClaimTypes.AuthProvider, provider.ToString())
        };

        if (!string.IsNullOrEmpty(response.RefreshToken))
        {
            claims.Add(new Claim("Ofertio:refresh_token", response.RefreshToken));
        }

        claims.AddRange(ClaimsFromToken(response.Token));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    /// <summary>Papel, e-mail e tenant do JWT da API, para menus e políticas na UI.</summary>
    public static IEnumerable<Claim> ClaimsFromToken(string? token)
    {
        var payload = JwtPayloadReader.Read(token);
        var role = JwtPayloadReader.Get(payload, JwtPayloadReader.RoleClaim, "role");
        if (!string.IsNullOrWhiteSpace(role))
        {
            yield return new Claim(ClaimTypes.Role, role);
        }

        var email = JwtPayloadReader.Get(payload, JwtPayloadReader.EmailClaim, "email");
        if (!string.IsNullOrWhiteSpace(email))
        {
            yield return new Claim(ClaimTypes.Email, email);
        }

        var tenantId = JwtPayloadReader.Get(payload, OfertioClaimTypes.TenantId);
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            yield return new Claim(OfertioClaimTypes.TenantId, tenantId);
        }
    }
}

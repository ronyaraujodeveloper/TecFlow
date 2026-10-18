using System.Security.Claims;
using Ofertio.SharedUi.Models.Enums;
using Ofertio.SharedUi.Models.Responses;

namespace Ofertio.SharedUi.Services.Auth;

public interface IAuthCookieService
{
    Task SignInFromAuthResponseAsync(
        AuthTokenResponse response,
        LoginPlatform platform,
        AuthProvider provider,
        CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);

    void SyncSessionFromPrincipal(ClaimsPrincipal principal);
}

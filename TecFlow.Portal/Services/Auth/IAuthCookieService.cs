using System.Security.Claims;
using TecFlow.Portal.Models.Enums;
using TecFlow.Portal.Models.Responses;

namespace TecFlow.Portal.Services.Auth;

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

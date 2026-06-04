using System.Security.Claims;
using TecFlow.WebUi.Models.Enums;
using TecFlow.WebUi.Models.Responses;

namespace TecFlow.WebUi.Services.Auth;

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

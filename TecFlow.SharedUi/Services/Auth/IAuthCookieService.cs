using System.Security.Claims;
using TecFlow.SharedUi.Models.Enums;
using TecFlow.SharedUi.Models.Responses;

namespace TecFlow.SharedUi.Services.Auth;

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

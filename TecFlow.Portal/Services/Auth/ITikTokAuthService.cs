using TecFlow.Portal.Models;
using TecFlow.Portal.Models.Enums;
using TecFlow.Portal.Models.Responses;

namespace TecFlow.Portal.Services.Auth;

public interface ITikTokAuthService
{
    Task<ApiResult<AuthTokenResponse>> LoginAsync(
        AuthProvider provider,
        string? accessToken,
        string? idToken,
        string? email,
        string? password,
        CancellationToken cancellationToken = default);
}

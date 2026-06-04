using TecFlow.WebUi.Models;
using TecFlow.WebUi.Models.Enums;
using TecFlow.WebUi.Models.Responses;

namespace TecFlow.WebUi.Services.Auth;

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

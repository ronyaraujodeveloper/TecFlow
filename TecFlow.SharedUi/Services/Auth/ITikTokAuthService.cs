using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Models.Enums;
using TecFlow.SharedUi.Models.Responses;

namespace TecFlow.SharedUi.Services.Auth;

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

using Ofertio.SharedUi.Models;
using Ofertio.SharedUi.Models.Enums;
using Ofertio.SharedUi.Models.Responses;

namespace Ofertio.SharedUi.Services.Auth;

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

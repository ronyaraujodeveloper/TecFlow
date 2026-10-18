using Ofertio.SharedUi.Models;
using Ofertio.SharedUi.Models.Enums;
using Ofertio.SharedUi.Models.Requests;
using Ofertio.SharedUi.Models.Responses;

namespace Ofertio.SharedUi.Services.Auth;

public interface IOrquestradorAuthBridge
{
    Task<ApiResult<AuthTokenResponse>> LoginAsync(
        LoginPlatform platform,
        PlatformAuthRequest request,
        CancellationToken cancellationToken = default);
}

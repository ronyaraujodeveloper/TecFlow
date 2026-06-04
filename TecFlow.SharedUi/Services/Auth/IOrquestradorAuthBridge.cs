using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Models.Enums;
using TecFlow.SharedUi.Models.Requests;
using TecFlow.SharedUi.Models.Responses;

namespace TecFlow.SharedUi.Services.Auth;

public interface IOrquestradorAuthBridge
{
    Task<ApiResult<AuthTokenResponse>> LoginAsync(
        LoginPlatform platform,
        PlatformAuthRequest request,
        CancellationToken cancellationToken = default);
}

using TecFlow.WebUi.Models;
using TecFlow.WebUi.Models.Enums;
using TecFlow.WebUi.Models.Requests;
using TecFlow.WebUi.Models.Responses;

namespace TecFlow.WebUi.Services.Auth;

public interface IOrquestradorAuthBridge
{
    Task<ApiResult<AuthTokenResponse>> LoginAsync(
        LoginPlatform platform,
        PlatformAuthRequest request,
        CancellationToken cancellationToken = default);
}

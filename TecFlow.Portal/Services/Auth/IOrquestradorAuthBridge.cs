using TecFlow.Portal.Models;
using TecFlow.Portal.Models.Enums;
using TecFlow.Portal.Models.Requests;
using TecFlow.Portal.Models.Responses;

namespace TecFlow.Portal.Services.Auth;

public interface IOrquestradorAuthBridge
{
    Task<ApiResult<AuthTokenResponse>> LoginAsync(
        LoginPlatform platform,
        PlatformAuthRequest request,
        CancellationToken cancellationToken = default);
}

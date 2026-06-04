using System.Security.Claims;
using TecFlow.SharedUi.Security;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.State;

namespace TecFlow.WebUi.Services.Http;

public class WebAccessTokenProvider : IAccessTokenProvider
{
    private readonly ISessionStateService _sessionState;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WebAccessTokenProvider(
        ISessionStateService sessionState,
        IHttpContextAccessor httpContextAccessor)
    {
        _sessionState = sessionState;
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetAccessToken()
    {
        if (!string.IsNullOrEmpty(_sessionState.AccessToken))
        {
            return _sessionState.AccessToken;
        }

        return _httpContextAccessor.HttpContext?.User.FindFirstValue(TecFlowClaimTypes.AccessToken);
    }
}

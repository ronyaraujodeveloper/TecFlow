using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TecFlow.Core.Security;
using TecFlow.SharedUi.Services.Auth;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.State;

namespace TecFlow.WebUi.Services.Http;

public class WebAccessTokenProvider : IAccessTokenProvider
{
    private readonly ISessionStateService _sessionState;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IAuthCookieService _authCookieService;

    public WebAccessTokenProvider(
        ISessionStateService sessionState,
        IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProvider authenticationStateProvider,
        IAuthCookieService authCookieService)
    {
        _sessionState = sessionState;
        _httpContextAccessor = httpContextAccessor;
        _authenticationStateProvider = authenticationStateProvider;
        _authCookieService = authCookieService;
    }

    public string? GetAccessToken() =>
        FirstNonEmpty(
            _sessionState.AccessToken,
            ReadAccessToken(_httpContextAccessor.HttpContext?.User));

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = GetAccessToken();
        if (!string.IsNullOrWhiteSpace(token))
        {
            return token;
        }

        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        _authCookieService.SyncSessionFromPrincipal(state.User);
        return FirstNonEmpty(
            _sessionState.AccessToken,
            ReadAccessToken(state.User),
            ReadAccessToken(_httpContextAccessor.HttpContext?.User));
    }

    private static string? ReadAccessToken(ClaimsPrincipal? user) =>
        user?.FindFirst(TecFlowClaimTypes.AccessToken)?.Value;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}

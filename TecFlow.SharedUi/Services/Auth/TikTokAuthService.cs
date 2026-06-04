using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Models.Enums;
using TecFlow.SharedUi.Models.Requests;
using TecFlow.SharedUi.Models.Responses;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.State;
using TecFlow.SharedUi.Services.UI;
using TecFlow.Util.Validation;

namespace TecFlow.SharedUi.Services.Auth;

public class TikTokAuthService : ITikTokAuthService
{
    private const string LoginEndpoint = "api/auth/tiktok/login";

    private readonly IHttpService _httpService;
    private readonly ISessionStateService _sessionState;
    private readonly ILoadingService _loadingService;

    public TikTokAuthService(
        IHttpService httpService,
        ISessionStateService sessionState,
        ILoadingService loadingService)
    {
        _httpService = httpService;
        _sessionState = sessionState;
        _loadingService = loadingService;
    }

    public async Task<ApiResult<AuthTokenResponse>> LoginAsync(
        AuthProvider provider,
        string? accessToken,
        string? idToken,
        string? email,
        string? password,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("A autenticar com TikTok...");

        if (provider == AuthProvider.EmailPassword && !ValidationHelper.IsValidEmail(email))
        {
            return ApiResult<AuthTokenResponse>.Fail("E-mail inválido.");
        }

        var request = BuildRequest(provider, accessToken, idToken, email, password);
        var result = await _httpService.PostAsync<PlatformAuthRequest, AuthTokenResponse>(
            LoginEndpoint,
            request,
            cancellationToken);

        if (result.Success && result.Data is not null)
        {
            _sessionState.SetSession(
                result.Data.Token,
                LoginPlatform.TikTok,
                result.Data.RefreshToken,
                result.Data.UserId,
                result.Data.DisplayName);
        }

        return result;
    }

    private static PlatformAuthRequest BuildRequest(
        AuthProvider provider,
        string? accessToken,
        string? idToken,
        string? email,
        string? password) =>
        new()
        {
            Provider = provider.ToString(),
            AccessToken = accessToken,
            IdToken = idToken,
            Email = email,
            Password = password
        };
}

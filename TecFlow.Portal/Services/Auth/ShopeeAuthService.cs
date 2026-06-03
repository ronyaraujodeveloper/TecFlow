using TecFlow.Portal.Models;
using TecFlow.Portal.Models.Enums;
using TecFlow.Portal.Models.Requests;
using TecFlow.Portal.Models.Responses;
using TecFlow.Portal.Services.Http;
using TecFlow.Portal.Services.State;
using TecFlow.Portal.Services.UI;
using TecFlow.Util.Validation;

namespace TecFlow.Portal.Services.Auth;

public class ShopeeAuthService : IShopeeAuthService
{
    private const string LoginEndpoint = "api/auth/shopee/login";

    private readonly IHttpService _httpService;
    private readonly ISessionStateService _sessionState;
    private readonly ILoadingService _loadingService;

    public ShopeeAuthService(
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
        using var _ = _loadingService.BeginScope("A autenticar com Shopee...");

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
                LoginPlatform.Shopee,
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

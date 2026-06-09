using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Security;
using TecFlow.Util.Validation;

namespace TecFlow.Infrastructure.Services.Security;

public class SocialAuthTokenValidator : ISocialAuthTokenValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SocialAuthTokenValidator> _logger;

    public SocialAuthTokenValidator(
        IHttpClientFactory httpClientFactory,
        ILogger<SocialAuthTokenValidator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SocialAuthTokenPayload?> ValidateAsync(
        string provider,
        string? accessToken,
        string? idToken,
        CancellationToken cancellationToken = default)
    {
        var normalizedProvider = AuthProviderNames.Normalize(provider);
        if (normalizedProvider is null || !AuthProviderNames.IsSocialProvider(normalizedProvider))
        {
            return null;
        }

        return normalizedProvider switch
        {
            AuthProviderNames.Google => await ValidateGoogleAsync(idToken ?? accessToken, cancellationToken),
            AuthProviderNames.Facebook => await ValidateFacebookAsync(accessToken, cancellationToken),
            AuthProviderNames.Apple => ValidateAppleIdToken(idToken ?? accessToken),
            _ => null
        };
    }

    private async Task<SocialAuthTokenPayload?> ValidateGoogleAsync(
        string? token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(SocialAuthTokenValidator));
            using var response = await client.GetAsync(
                $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(token)}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token Google inválido. Status={StatusCode}", response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<GoogleTokenInfoResponse>(JsonOptions, cancellationToken);
            if (payload is null
                || string.IsNullOrWhiteSpace(payload.Sub)
                || string.IsNullOrWhiteSpace(payload.Email)
                || !ValidationHelper.IsValidEmail(payload.Email))
            {
                return null;
            }

            return new SocialAuthTokenPayload
            {
                Email = payload.Email.Trim(),
                ProviderKey = payload.Sub.Trim(),
                DisplayName = payload.Name?.Trim()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao validar token Google.");
            return null;
        }
    }

    private async Task<SocialAuthTokenPayload?> ValidateFacebookAsync(
        string? accessToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(SocialAuthTokenValidator));
            using var response = await client.GetAsync(
                $"https://graph.facebook.com/me?fields=id,name,email&access_token={Uri.EscapeDataString(accessToken)}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token Facebook inválido. Status={StatusCode}", response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<FacebookProfileResponse>(JsonOptions, cancellationToken);
            if (payload is null
                || string.IsNullOrWhiteSpace(payload.Id)
                || string.IsNullOrWhiteSpace(payload.Email)
                || !ValidationHelper.IsValidEmail(payload.Email))
            {
                return null;
            }

            return new SocialAuthTokenPayload
            {
                Email = payload.Email.Trim(),
                ProviderKey = payload.Id.Trim(),
                DisplayName = payload.Name?.Trim()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao validar token Facebook.");
            return null;
        }
    }

    private SocialAuthTokenPayload? ValidateAppleIdToken(string? idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(idToken);
            var email = jwt.Claims.FirstOrDefault(claim => claim.Type == "email")?.Value;
            var subject = jwt.Subject;

            if (string.IsNullOrWhiteSpace(subject)
                || string.IsNullOrWhiteSpace(email)
                || !ValidationHelper.IsValidEmail(email))
            {
                return null;
            }

            return new SocialAuthTokenPayload
            {
                Email = email.Trim(),
                ProviderKey = subject.Trim(),
                DisplayName = jwt.Claims.FirstOrDefault(claim => claim.Type == "name")?.Value
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao validar id_token Apple.");
            return null;
        }
    }

    private sealed class GoogleTokenInfoResponse
    {
        public string? Sub { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
    }

    private sealed class FacebookProfileResponse
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
    }
}

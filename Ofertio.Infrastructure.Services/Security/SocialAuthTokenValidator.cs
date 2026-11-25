using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Security;
using Ofertio.Business.Service.Security;
using Ofertio.Util.Validation;

namespace Ofertio.Infrastructure.Services.Security;

/// <summary>
/// Valida tokens de login social. Todo provedor precisa estar configurado na API
/// (Authentication:{Provedor}:ClientId); sem configuração o token é recusado.
/// </summary>
public class SocialAuthTokenValidator : ISocialAuthTokenValidator
{
    public const string AppleIssuer = "https://appleid.apple.com";
    public const string AppleKeysUrl = "https://appleid.apple.com/auth/keys";
    public const string FacebookGraphUrl = "https://graph.facebook.com";

    private static readonly TimeSpan AppleKeysLifetime = TimeSpan.FromHours(6);
    private static readonly SemaphoreSlim AppleKeysLock = new(1, 1);
    private static AppleKeyCache? _appleKeys;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SocialAuthTokenValidator> _logger;
    private readonly string? _googleClientId;
    private readonly string? _appleClientId;
    private readonly string? _facebookAppId;
    private readonly string? _facebookAppSecret;

    public SocialAuthTokenValidator(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SocialAuthTokenValidator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _googleClientId = configuration["Authentication:Google:ClientId"];
        _appleClientId = configuration["Authentication:Apple:ClientId"];
        _facebookAppId = configuration["Authentication:Facebook:ClientId"];
        _facebookAppSecret = configuration["Authentication:Facebook:ClientSecret"];
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
            AuthProviderNames.Google => await ValidateGoogleAsync(idToken, cancellationToken),
            AuthProviderNames.Facebook => await ValidateFacebookAsync(accessToken, cancellationToken),
            AuthProviderNames.Apple => await ValidateAppleIdTokenAsync(idToken ?? accessToken, cancellationToken),
            _ => null
        };
    }

    private async Task<SocialAuthTokenPayload?> ValidateGoogleAsync(
        string? token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token Google recusado: id_token ausente. O access_token não é aceito em tokeninfo.");
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(SocialAuthTokenValidator));
            using var response = await client.GetAsync(
                $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(token)}",
                cancellationToken);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var detail = await DescribeGoogleErrorAsync(response, cancellationToken);
                _logger.LogWarning(
                    "Token Google recusado pelo tokeninfo. Status={StatusCode}. {Detail}",
                    (int)response.StatusCode,
                    detail);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<GoogleTokenInfoResponse>(JsonOptions, cancellationToken);
            if (payload is null
                || string.IsNullOrWhiteSpace(payload.Sub)
                || string.IsNullOrWhiteSpace(payload.Email)
                || !ValidationHelper.IsValidEmail(payload.Email))
            {
                _logger.LogWarning(
                    "Token Google recusado: tokeninfo sem sub ou e-mail válido. TemSub={HasSub} TemEmail={HasEmail}",
                    !string.IsNullOrWhiteSpace(payload?.Sub),
                    !string.IsNullOrWhiteSpace(payload?.Email));
                return null;
            }

            if (string.IsNullOrWhiteSpace(_googleClientId))
            {
                _logger.LogWarning("Token Google recusado: Authentication:Google:ClientId não está configurado na API.");
                return null;
            }

            if (!GoogleIdTokenRules.AudienceMatches(payload.Aud, _googleClientId))
            {
                _logger.LogWarning(
                    "Token Google recusado: aud divergente. Aud={Audience}",
                    payload.Aud);
                return null;
            }

            return new SocialAuthTokenPayload
            {
                Email = payload.Email.Trim(),
                ProviderKey = payload.Sub.Trim(),
                DisplayName = payload.Name?.Trim(),
                EmailVerified = SocialAccountLinkRules.IsTrue(payload.EmailVerified)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha HTTP ao validar o id_token do Google.");
            return null;
        }
    }

    private static async Task<string> DescribeGoogleErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var error = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null;
            var description = root.TryGetProperty("error_description", out var descriptionElement)
                ? descriptionElement.GetString()
                : null;
            return $"error={error}; description={description}";
        }
        catch (Exception ex)
        {
            return "sem detalhe do Google (" + ex.GetType().Name + ")";
        }
    }

    /// <summary>
    /// Confere no debug_token que o access token foi emitido para o app do Ofertio
    /// (um token de outro app com o e-mail da vítima não serve) e só então lê o perfil.
    /// </summary>
    private async Task<SocialAuthTokenPayload?> ValidateFacebookAsync(
        string? accessToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_facebookAppId) || string.IsNullOrWhiteSpace(_facebookAppSecret))
        {
            _logger.LogWarning("Token Facebook recusado: Authentication:Facebook:ClientId/ClientSecret não estão configurados na API.");
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(SocialAuthTokenValidator));

            // Token do app no cabeçalho, nunca na query: assim o segredo não aparece em logs de URL.
            using var debugRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{FacebookGraphUrl}/debug_token?input_token={Uri.EscapeDataString(accessToken)}");
            debugRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", $"{_facebookAppId}|{_facebookAppSecret}");
            using var debugResponse = await client.SendAsync(debugRequest, cancellationToken);
            if (!debugResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token Facebook recusado pelo debug_token. Status={StatusCode}", (int)debugResponse.StatusCode);
                return null;
            }

            var debug = await debugResponse.Content.ReadFromJsonAsync<FacebookDebugTokenResponse>(JsonOptions, cancellationToken);
            if (debug?.Data is not { IsValid: true } data
                || !string.Equals(data.AppId, _facebookAppId, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(data.UserId))
            {
                _logger.LogWarning(
                    "Token Facebook recusado: inválido ou emitido para outro app. AppId={AppId}",
                    debug?.Data?.AppId);
                return null;
            }

            using var response = await client.GetAsync(
                $"{FacebookGraphUrl}/me?fields=id,name,email&access_token={Uri.EscapeDataString(accessToken)}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token Facebook inválido. Status={StatusCode}", response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<FacebookProfileResponse>(JsonOptions, cancellationToken);
            if (payload is null
                || string.IsNullOrWhiteSpace(payload.Id)
                || !string.Equals(payload.Id, data.UserId, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(payload.Email)
                || !ValidationHelper.IsValidEmail(payload.Email))
            {
                return null;
            }

            return new SocialAuthTokenPayload
            {
                Email = payload.Email.Trim(),
                ProviderKey = payload.Id.Trim(),
                DisplayName = payload.Name?.Trim(),
                EmailVerified = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao validar token Facebook.");
            return null;
        }
    }

    /// <summary>
    /// Valida assinatura (chaves públicas da Apple), issuer, audience e validade do id_token.
    /// Antes o token só era decodificado: qualquer JWT montado à mão com o e-mail da vítima entrava.
    /// </summary>
    private async Task<SocialAuthTokenPayload?> ValidateAppleIdTokenAsync(string? idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_appleClientId))
        {
            _logger.LogWarning("Token Apple recusado: Authentication:Apple:ClientId não está configurado na API.");
            return null;
        }

        try
        {
            var handler = new JsonWebTokenHandler();
            if (!handler.CanReadToken(idToken))
            {
                return null;
            }

            var keyId = handler.ReadJsonWebToken(idToken).Kid;
            var keys = await GetAppleSigningKeysAsync(keyId, cancellationToken);
            if (keys.Count == 0)
            {
                _logger.LogWarning("Token Apple recusado: nenhuma chave pública da Apple disponível.");
                return null;
            }

            var result = await handler.ValidateTokenAsync(idToken, new TokenValidationParameters
            {
                ValidIssuer = AppleIssuer,
                ValidAudience = _appleClientId.Trim(),
                IssuerSigningKeys = keys,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            });

            if (!result.IsValid)
            {
                _logger.LogWarning(
                    "Token Apple recusado: {Reason}.",
                    result.Exception?.GetType().Name ?? "validação falhou");
                return null;
            }

            var email = ClaimAsString(result.Claims, "email");
            var subject = ClaimAsString(result.Claims, "sub");
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
                DisplayName = ClaimAsString(result.Claims, "name"),
                EmailVerified = SocialAccountLinkRules.IsTrue(result.Claims.TryGetValue("email_verified", out var verified) ? verified : null)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao validar id_token Apple.");
            return null;
        }
    }

    /// <summary>Chaves públicas da Apple em cache; busca de novo quando vencem ou quando chega um kid desconhecido (rotação).</summary>
    private async Task<IList<SecurityKey>> GetAppleSigningKeysAsync(string? keyId, CancellationToken cancellationToken)
    {
        var cached = _appleKeys;
        if (cached is not null && cached.IsFresh && cached.Contains(keyId))
        {
            return cached.Keys;
        }

        await AppleKeysLock.WaitAsync(cancellationToken);
        try
        {
            cached = _appleKeys;
            if (cached is not null && cached.IsFresh && cached.Contains(keyId))
            {
                return cached.Keys;
            }

            var client = _httpClientFactory.CreateClient(nameof(SocialAuthTokenValidator));
            var json = await client.GetStringAsync(AppleKeysUrl, cancellationToken);
            var set = new JsonWebKeySet(json);
            _appleKeys = new AppleKeyCache(set.GetSigningKeys(), DateTime.UtcNow.Add(AppleKeysLifetime));
            return _appleKeys.Keys;
        }
        finally
        {
            AppleKeysLock.Release();
        }
    }

    private static string? ClaimAsString(IDictionary<string, object> claims, string name) =>
        claims.TryGetValue(name, out var value) ? value?.ToString() : null;

    private sealed record AppleKeyCache(IList<SecurityKey> Keys, DateTime ExpiresAt)
    {
        public bool IsFresh => DateTime.UtcNow < ExpiresAt;

        public bool Contains(string? keyId) =>
            string.IsNullOrEmpty(keyId) || Keys.Any(key => string.Equals(key.KeyId, keyId, StringComparison.Ordinal));
    }

    private sealed class GoogleTokenInfoResponse
    {
        public string? Sub { get; set; }
        public string? Aud { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }

        /// <summary>O tokeninfo devolve "true" (texto); JsonElement aceita texto ou booleano sem quebrar a leitura.</summary>
        [JsonPropertyName("email_verified")]
        public JsonElement? EmailVerified { get; set; }
    }

    private sealed class FacebookDebugTokenResponse
    {
        public FacebookDebugTokenData? Data { get; set; }
    }

    private sealed class FacebookDebugTokenData
    {
        [JsonPropertyName("app_id")]
        public string? AppId { get; set; }

        [JsonPropertyName("is_valid")]
        public bool IsValid { get; set; }

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }
    }

    private sealed class FacebookProfileResponse
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
    }
}

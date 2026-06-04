using System.Text.Json.Serialization;

namespace TecFlow.Infrastructure.Services.Integrations.Auth;

internal sealed class MarketplaceOAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expire_in")]
    public int? ExpireIn { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token_expire_in")]
    public int? RefreshTokenExpireIn { get; set; }

    [JsonPropertyName("data")]
    public MarketplaceOAuthTokenData? Data { get; set; }

    public int ResolveAccessTokenLifetimeSeconds() =>
        ExpireIn ?? ExpiresIn ?? Data?.ExpireIn ?? Data?.ExpiresIn ?? 3600;

    public string? ResolveAccessToken() =>
        AccessToken ?? Data?.AccessToken;

    public string? ResolveRefreshToken() =>
        RefreshToken ?? Data?.RefreshToken;
}

internal sealed class MarketplaceOAuthTokenData
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expire_in")]
    public int? ExpireIn { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token_expire_in")]
    public int? RefreshTokenExpireIn { get; set; }
}

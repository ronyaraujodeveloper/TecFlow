using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ofertio.Infrastructure.Services.Integrations.Auth;

internal sealed class MarketplaceOAuthTokenResponse
{
    /// <summary>Valores acima disso são datas Unix absolutas (TikTok Shop), não quantidade de segundos.</summary>
    private const long UnixTimestampThreshold = 1_000_000_000;

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expire_in")]
    public int? ExpireIn { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token_expire_in")]
    public long? RefreshTokenExpireIn { get; set; }

    /// <summary>Envelope TikTok Shop: 0 = sucesso.</summary>
    [JsonPropertyName("code")]
    public JsonElement? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public MarketplaceOAuthTokenData? Data { get; set; }

    public int ResolveAccessTokenLifetimeSeconds(DateTime utcNow)
    {
        if (Data?.AccessTokenExpireIn is { } tikTokExpire)
        {
            return ToLifetimeSeconds(tikTokExpire, utcNow, 3600);
        }

        return ExpireIn ?? ExpiresIn ?? Data?.ExpireIn ?? Data?.ExpiresIn ?? 3600;
    }

    public int ResolveRefreshTokenLifetimeSeconds(DateTime utcNow)
    {
        var value = RefreshTokenExpireIn ?? Data?.RefreshTokenExpireIn;
        return value is null ? 0 : ToLifetimeSeconds(value.Value, utcNow, 0);
    }

    public string? ResolveAccessToken() =>
        AccessToken ?? Data?.AccessToken;

    public string? ResolveRefreshToken() =>
        RefreshToken ?? Data?.RefreshToken;

    /// <summary>Código de erro do envelope TikTok Shop (null quando ausente ou 0).</summary>
    public long? ResolveErrorCode()
    {
        if (Code is not { } code)
        {
            return null;
        }

        var value = code.ValueKind switch
        {
            JsonValueKind.Number when code.TryGetInt64(out var number) => number,
            JsonValueKind.String when long.TryParse(code.GetString(), out var parsed) => parsed,
            _ => 0
        };
        return value == 0 ? null : value;
    }

    private static int ToLifetimeSeconds(long value, DateTime utcNow, int fallback)
    {
        if (value <= 0)
        {
            return fallback;
        }

        if (value < UnixTimestampThreshold)
        {
            return (int)Math.Min(value, int.MaxValue);
        }

        var seconds = DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime - utcNow;
        return seconds.TotalSeconds <= 0 ? 0 : (int)Math.Min(seconds.TotalSeconds, int.MaxValue);
    }
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

    [JsonPropertyName("access_token_expire_in")]
    public long? AccessTokenExpireIn { get; set; }

    [JsonPropertyName("refresh_token_expire_in")]
    public long? RefreshTokenExpireIn { get; set; }

    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    [JsonPropertyName("seller_name")]
    public string? SellerName { get; set; }

    /// <summary>TikTok Shop: 0 = vendedor, 1 = criador.</summary>
    [JsonPropertyName("user_type")]
    public int? UserType { get; set; }
}

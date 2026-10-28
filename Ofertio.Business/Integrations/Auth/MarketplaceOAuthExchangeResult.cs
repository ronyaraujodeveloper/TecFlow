namespace Ofertio.Business.Integrations.Auth;

public sealed class MarketplaceOAuthExchangeResult
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public string AccessToken { get; init; } = string.Empty;

    public string? RefreshToken { get; init; }

    public DateTime ExpiresAt { get; init; }

    public DateTime? RefreshExpiresAt { get; init; }

    /// <summary>Identificador da conta autorizada (TikTok Shop: <c>open_id</c>).</summary>
    public string? OpenId { get; init; }

    public string? AccountName { get; init; }

    /// <summary>TikTok Shop: 0 = vendedor, 1 = criador (<see cref="TikTokShop.TikTokShopUserTypes"/>).</summary>
    public int? UserType { get; init; }

    public static MarketplaceOAuthExchangeResult Fail(string message) =>
        new()
        {
            Success = false,
            ErrorMessage = message
        };
}

namespace TecFlow.Business.Integrations.Auth;

public sealed class MarketplaceOAuthExchangeResult
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public string AccessToken { get; init; } = string.Empty;

    public string? RefreshToken { get; init; }

    public DateTime ExpiresAt { get; init; }

    public DateTime? RefreshExpiresAt { get; init; }

    public static MarketplaceOAuthExchangeResult Fail(string message) =>
        new()
        {
            Success = false,
            ErrorMessage = message
        };
}

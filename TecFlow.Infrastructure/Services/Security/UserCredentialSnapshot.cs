namespace TecFlow.Infrastructure.Services.Security;

public sealed class UserCredentialSnapshot
{
    public int Id { get; init; }

    public string PasswordHash { get; init; } = string.Empty;

    public string? TikTokShopAccessToken { get; init; }

    public string? TikTokRefreshToken { get; init; }
}

namespace TecFlow.SharedUi.Models.Responses;

public class AuthTokenResponse
{
    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? UserId { get; set; }
    public string? DisplayName { get; set; }
    public string Platform { get; set; } = string.Empty;
}

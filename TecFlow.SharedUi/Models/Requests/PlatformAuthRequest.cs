namespace TecFlow.SharedUi.Models.Requests;

public class PlatformAuthRequest
{
    public string Provider { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? IdToken { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

namespace TecFlow.Business.Dto.Auth;

public class LinkProviderDto
{
    public string Provider { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? IdToken { get; set; }
}

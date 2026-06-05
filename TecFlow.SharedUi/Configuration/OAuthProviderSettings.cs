namespace TecFlow.SharedUi.Configuration;

public class OAuthProviderSettings
{
    public bool Enabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class AppleOAuthSettings : OAuthProviderSettings
{
    public string KeyId { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    /// <summary>Conteúdo PEM da chave (.p8) ou caminho absoluto para o arquivo.</summary>
    public string PrivateKey { get; set; } = string.Empty;
}

public class PortalAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public int CookieExpireHours { get; set; } = 8;
    public OAuthProviderSettings Google { get; set; } = new();
    public OAuthProviderSettings Facebook { get; set; } = new();
    public AppleOAuthSettings Apple { get; set; } = new();
}

namespace TecFlow.Business.Integrations.MercadoLivre;

public sealed class MercadoLivreIntegrationOptions
{
    public const string SectionName = "Integrations:MercadoLivre";

    public string AppId { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string AccessToken { get; set; } = string.Empty;

    public string TokenUrl { get; set; } = "https://api.mercadolibre.com/oauth/token";

    public bool HasAppCredentials =>
        !string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(SecretKey);

    public bool HasAccessToken => !string.IsNullOrWhiteSpace(AccessToken);
}

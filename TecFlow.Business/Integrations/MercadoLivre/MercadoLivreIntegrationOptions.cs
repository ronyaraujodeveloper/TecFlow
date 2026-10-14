namespace TecFlow.Business.Integrations.MercadoLivre;

public sealed class MercadoLivreIntegrationOptions
{
    public const string SectionName = "Integrations:MercadoLivre";

    public string AppId { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string AccessToken { get; set; } = string.Empty;

    public string TokenUrl { get; set; } = "https://api.mercadolibre.com/oauth/token";

    public string AuthorizeUrl { get; set; } = "https://auth.mercadolivre.com.br/authorization";

    /// <summary>
    /// Redirect URI HTTPS cadastrada no app do developers.mercadolivre.com.br.
    /// Quando vazia, usa o callback da origem atual (/integracoes/oauth/callback).
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Desligado enquanto o app aguarda homologação no portal do ML (a API devolve 403 PA_UNAUTHORIZED_RESULT_FROM_POLICIES):
    /// a busca usa a vitrine pública lista.mercadolivre.com.br como motor primário.
    /// </summary>
    public bool UseOfficialSearchApi { get; set; }

    public bool HasAppCredentials =>
        !string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(SecretKey);

    public bool HasAccessToken => !string.IsNullOrWhiteSpace(AccessToken);
}

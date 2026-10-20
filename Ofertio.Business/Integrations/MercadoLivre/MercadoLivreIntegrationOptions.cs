namespace Ofertio.Business.Integrations.MercadoLivre;

public sealed class MercadoLivreIntegrationOptions
{
    public const string SectionName = "Integrations:MercadoLivre";

    /// <summary>matt_tool oficial do Ofertio, aplicado quando nenhum outro TrackingId estiver configurado.</summary>
    public const string OfficialAffiliateTrackingId = "97303724";

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
    /// matt_tool usado no "Abrir Link Original" quando o tenant não tem conta Mercado Livre com TrackingId.
    /// </summary>
    public string DefaultAffiliateTrackingId { get; set; } = OfficialAffiliateTrackingId;

    public bool HasAppCredentials =>
        !string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(SecretKey);

    public bool HasAccessToken => !string.IsNullOrWhiteSpace(AccessToken);
}

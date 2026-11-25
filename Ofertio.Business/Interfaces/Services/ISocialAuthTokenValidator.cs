namespace Ofertio.Business.Interfaces.Services;

/// <summary>Valida tokens sociais e extrai e-mail/subject do provedor.</summary>
public interface ISocialAuthTokenValidator
{
    Task<SocialAuthTokenPayload?> ValidateAsync(
        string provider,
        string? accessToken,
        string? idToken,
        CancellationToken cancellationToken = default);
}

public sealed class SocialAuthTokenPayload
{
    public required string Email { get; init; }
    public required string ProviderKey { get; init; }
    public string? DisplayName { get; init; }

    /// <summary>
    /// O provedor afirma que o e-mail foi verificado (Google e Apple trazem email_verified;
    /// o Facebook não expõe essa informação e fica sempre falso).
    /// </summary>
    public bool EmailVerified { get; init; }
}

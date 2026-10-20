using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Integrations.Auth;

/// <summary>Contexto selado no parâmetro <c>state</c> do OAuth: identifica quem autorizou sem depender da sessão do navegador.</summary>
public sealed record MarketplaceOAuthState(
    int UserId,
    Guid? TenantId,
    MarketplaceType Platform,
    string FriendlyName,
    string? CallbackUri,
    string Nonce,
    DateTimeOffset ExpiresAt,
    string? CodeVerifier = null);

/// <summary>Sela (criptografia autenticada) e abre o <c>state</c> do OAuth.</summary>
public interface IOAuthStateProtector
{
    string Protect(MarketplaceOAuthState state);

    /// <summary>Falso para valor adulterado, de outra chave, expirado ou não selado pelo Ofertio.</summary>
    bool TryUnprotect(string? protectedState, out MarketplaceOAuthState? state);
}

public static class MarketplaceOAuthStateRules
{
    public const string Prefix = "tfs1.";

    public const string AccountsPath = "/marketplace-accounts";

    public const string InvalidStateMessage =
        "Link de autorização inválido ou expirado. Clique em Conectar novamente.";

    public const string ReplayedStateMessage =
        "Esta autorização já foi utilizada. Clique em Conectar novamente.";

    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    public static bool IsProtected(string? state) =>
        !string.IsNullOrWhiteSpace(state) && state.Trim().StartsWith(Prefix, StringComparison.Ordinal);

    public static MarketplaceOAuthState Create(
        int userId,
        Guid? tenantId,
        MarketplaceType platform,
        string? friendlyName,
        string? callbackUri,
        DateTimeOffset? now = null) =>
        new(
            userId,
            tenantId,
            platform,
            string.IsNullOrWhiteSpace(friendlyName) ? "Mercado Livre" : friendlyName.Trim(),
            string.IsNullOrWhiteSpace(callbackUri) ? null : callbackUri.Trim(),
            Guid.NewGuid().ToString("N"),
            (now ?? DateTimeOffset.UtcNow).Add(Lifetime),
            platform == MarketplaceType.MercadoLivre ? MercadoLivrePkce.CreateCodeVerifier() : null);

    /// <summary>Retorno para a origem onde o OAuth começou (ex.: https://localhost:7002/marketplace-accounts?status=success).</summary>
    public static string BuildReturnUrl(string? callbackUri, bool success, string? message = null)
    {
        var query = success
            ? "?status=success"
            : "?status=error&message=" + Uri.EscapeDataString(string.IsNullOrWhiteSpace(message) ? "Não foi possível vincular a loja." : message.Trim());
        var origin = Uri.TryCreate(callbackUri, UriKind.Absolute, out var uri)
                     && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri.GetLeftPart(UriPartial.Authority)
            : string.Empty;
        return origin + AccountsPath + query;
    }
}

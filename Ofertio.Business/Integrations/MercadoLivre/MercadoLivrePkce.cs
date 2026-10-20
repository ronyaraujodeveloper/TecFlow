using System.Security.Cryptography;
using System.Text;

namespace Ofertio.Business.Integrations.MercadoLivre;

/// <summary>
/// PKCE (RFC 7636) exigido pelo app do Mercado Livre: <c>code_challenge</c> S256 na URL de autorização e o
/// <c>code_verifier</c> correspondente na troca do code em <c>/oauth/token</c>.
/// </summary>
public static class MercadoLivrePkce
{
    public const string ChallengeMethod = "S256";

    /// <summary>64 caracteres base64url (dentro do intervalo 43–128 do RFC 7636).</summary>
    public static string CreateCodeVerifier() => ToBase64Url(RandomNumberGenerator.GetBytes(48));

    public static string ComputeCodeChallenge(string codeVerifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeVerifier);
        return ToBase64Url(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier.Trim())));
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

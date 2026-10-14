namespace TecFlow.Business.Integrations.Auth;

/// <summary>Limpa a redirect_uri do OAuth antes de enviá-la à plataforma (deve bater byte a byte com a cadastrada no app).</summary>
public static class OAuthRedirectUriNormalizer
{
    public const string CallbackPath = "/integracoes/oauth/callback";

    private static readonly string[] TunnelHostSuffixes =
    [
        ".ngrok-free.dev",
        ".ngrok-free.app",
        ".ngrok.io",
        ".ngrok.app",
        ".ngrok.dev"
    ];

    public static bool IsTunnelHost(string? host) =>
        !string.IsNullOrWhiteSpace(host)
        && TunnelHostSuffixes.Any(suffix => host.Trim().EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Túneis Ngrok: força https, remove a porta local (:7002 / :5000), query e fragmento,
    /// e completa o caminho do callback quando só a origem foi informada.
    /// Demais hosts são devolvidos apenas sem espaços.
    /// </summary>
    public static string Normalize(string? redirectUri)
    {
        var raw = redirectUri?.Trim() ?? string.Empty;
        if (raw.Length == 0
            || !Uri.TryCreate(raw, UriKind.Absolute, out var uri)
            || !IsTunnelHost(uri.Host))
        {
            return raw;
        }

        var path = uri.AbsolutePath.TrimEnd('/');
        if (path.Length == 0)
        {
            path = CallbackPath;
        }

        return $"{Uri.UriSchemeHttps}://{uri.Host.ToLowerInvariant()}{path}";
    }
}

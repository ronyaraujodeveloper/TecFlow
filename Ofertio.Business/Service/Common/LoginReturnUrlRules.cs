namespace Ofertio.Business.Service.Common;

/// <summary>
/// Endereço para onde voltar depois do login (ex.: a tela em que a sessão caiu).
/// Só aceita caminhos internos do site; qualquer outra coisa é descartada para não virar redirecionamento aberto.
/// </summary>
public static class LoginReturnUrlRules
{
    public const string QueryKey = "returnUrl";
    public const string LoginPath = "/entrar";
    public const string DefaultAfterLogin = "/dashboard";
    private const int MaxLength = 2048;

    private static readonly string[] BlockedPrefixes =
    [
        "/entrar", "/auth", "/cadastro", "/register", "/recuperar-senha", "/conheca"
    ];

    /// <summary>Devolve o caminho se for seguro; nulo se for vazio, externo, a raiz ou uma tela de login/cadastro.</summary>
    public static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var candidate = value.Trim();
        if (candidate.Length > MaxLength
            || candidate[0] != '/'
            || candidate.StartsWith("//", StringComparison.Ordinal)
            || candidate.Contains('\\')
            || candidate.Any(char.IsControl))
        {
            return null;
        }

        var path = candidate.Split('?', '#')[0].TrimEnd('/');
        if (path.Length == 0)
        {
            return null;
        }

        foreach (var blocked in BlockedPrefixes)
        {
            if (path.Equals(blocked, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(blocked + "/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        return candidate;
    }

    /// <summary>Acrescenta ?returnUrl= ao endereço quando o destino é seguro.</summary>
    public static string AppendTo(string url, string? returnUrl)
    {
        var safe = Sanitize(returnUrl);
        if (safe is null)
        {
            return url;
        }

        return url + (url.Contains('?') ? "&" : "?") + QueryKey + "=" + Uri.EscapeDataString(safe);
    }

    /// <summary>Caminho do login já levando a tela atual (relativa à raiz, com ou sem barra inicial) como destino.</summary>
    public static string BuildLoginPath(string? currentPath)
    {
        var normalized = string.IsNullOrWhiteSpace(currentPath)
            ? string.Empty
            : "/" + currentPath.TrimStart('/');
        return AppendTo(LoginPath, normalized);
    }

    public static string ResolveAfterLogin(string? returnUrl) => Sanitize(returnUrl) ?? DefaultAfterLogin;
}
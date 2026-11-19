using System.Text.RegularExpressions;

namespace Ofertio.Business.Service.PublicPages;

/// <summary>Canal de um botão "entrar no grupo". Para acrescentar outro canal basta uma linha em <see cref="JoinLinkRules.Channels"/>.</summary>
public sealed record JoinChannel(string Name, string Label, string Icon, string CssClass);

/// <summary>
/// Regras dos links de convite da página pública: canais aceitos, validação do endereço (só os domínios esperados),
/// estados e o texto do botão. Sem acesso a banco nem a integrações.
/// </summary>
public static class JoinLinkRules
{
    public const string WhatsApp = "WhatsApp";
    public const string Telegram = "Telegram";

    public const string StatusActive = "Active";
    public const string StatusInactive = "Inactive";
    public const string StatusUnverified = "Unverified";

    /// <summary>Falhas seguidas de conferência para desativar o link (uma queda isolada não derruba o botão).</summary>
    public const int FailuresToDeactivate = 2;

    public const int MaxLabelLength = 80;

    public static readonly IReadOnlyList<JoinChannel> Channels =
    [
        new(WhatsApp, "WhatsApp", "bi-whatsapp", "ch-whatsapp"),
        new(Telegram, "Telegram", "bi-telegram", "ch-telegram")
    ];

    private static readonly Regex WhatsAppInvite = new(@"^/[A-Za-z0-9]{16,32}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex WhatsAppChannel = new(@"^/channel/[A-Za-z0-9_\-]{8,64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex TelegramPrivate = new(@"^/(\+|joinchat/)[A-Za-z0-9_\-]{8,40}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex TelegramPublic = new(@"^/[A-Za-z][A-Za-z0-9_]{4,31}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static JoinChannel? FindChannel(string? name) =>
        Channels.FirstOrDefault(channel => string.Equals(channel.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static bool IsKnownChannel(string? name) => FindChannel(name) is not null;

    /// <summary>
    /// Valida e normaliza um link de convite. Só aceita https, sem usuário/senha, nos domínios do canal e no formato de convite
    /// (WhatsApp: chat.whatsapp.com/código ou whatsapp.com/channel/id; Telegram: t.me/+código, t.me/joinchat/código ou t.me/usuário).
    /// </summary>
    public static bool TryNormalizeInviteUrl(string? channel, string? url, out string normalized, out string? error)
    {
        normalized = string.Empty;
        error = null;
        var definition = FindChannel(channel);
        if (definition is null)
        {
            error = "Escolha WhatsApp ou Telegram.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(url) || url.Length > 512)
        {
            error = "Informe o link de convite do grupo.";
            return false;
        }

        var candidate = url.Trim();
        if (!candidate.Contains("://", StringComparison.Ordinal))
        {
            candidate = "https://" + candidate;
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            error = "O link precisa começar com https:// e ser um convite válido.";
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        var path = uri.AbsolutePath.TrimEnd('/');
        var valid = definition.Name == WhatsApp
            ? (host == "chat.whatsapp.com" && WhatsAppInvite.IsMatch(path) && string.IsNullOrEmpty(uri.Query))
                || ((host == "whatsapp.com" || host == "www.whatsapp.com") && WhatsAppChannel.IsMatch(path))
            : (host is "t.me" or "telegram.me")
                && (TelegramPrivate.IsMatch(path) || TelegramPublic.IsMatch(path));

        if (!valid)
        {
            error = definition.Name == WhatsApp
                ? "Use o link de convite do grupo (chat.whatsapp.com/…) ou do canal (whatsapp.com/channel/…)."
                : "Use o link de convite do Telegram (t.me/+… ou t.me/nome-do-grupo).";
            return false;
        }

        normalized = new UriBuilder(Uri.UriSchemeHttps, host == "www.whatsapp.com" ? "whatsapp.com" : host) { Path = path }.Uri.ToString();
        return true;
    }

    public static string NormalizeLabel(string? label, string fallback)
    {
        var text = Regex.Replace((label ?? string.Empty).Trim(), @"\s+", " ");
        if (text.Length == 0)
        {
            text = fallback.Trim();
        }

        return text.Length <= MaxLabelLength ? text : text[..MaxLabelLength].TrimEnd();
    }

    /// <summary>0 = ilimitado.</summary>
    public static bool ExceedsLimit(int limit, int currentCount) => limit > 0 && currentCount >= limit;

    /// <summary>Texto do botão: nome do grupo e, se o afiliado quis, o número de membros.</summary>
    public static string MemberText(bool show, int? count) =>
        show && count is > 0 ? FormatCount(count.Value) + " membros" : string.Empty;

    public static string FormatCount(int count) => count switch
    {
        >= 1_000_000 => (count / 1_000_000d).ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) + " mi",
        >= 1_000 => (count / 1_000d).ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) + " mil",
        _ => count.ToString(System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))
    };

    /// <summary>Resultado da conferência: sobe a contagem de falhas e decide o estado.</summary>
    public static (string Status, int FailedChecks, string? Reason) AfterFailedCheck(int previousFailures)
    {
        var failures = previousFailures + 1;
        return failures >= FailuresToDeactivate
            ? (StatusInactive, failures, "O grupo não foi encontrado ou a conta deixou de ser administradora.")
            : (StatusActive, failures, null);
    }

    public static bool IsPublic(string? status) =>
        string.Equals(status, StatusActive, StringComparison.Ordinal)
        || string.Equals(status, StatusUnverified, StringComparison.Ordinal);
}

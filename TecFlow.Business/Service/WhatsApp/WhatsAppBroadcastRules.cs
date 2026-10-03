using System.Text.Json;
using System.Text.RegularExpressions;
using TecFlow.Core.Entities;

namespace TecFlow.Business.Service.WhatsApp;

public static class WhatsAppBroadcastRules
{
    public const string CommissionTag = "[LINK_COMISSAO]";

    private static readonly Regex HttpUrlRegex = new(
        @"https?://[^\s]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static int ClampIntervalSeconds(int intervalSeconds)
    {
        if (intervalSeconds < WhatsAppBroadcastStatuses.MinIntervalSeconds)
        {
            return WhatsAppBroadcastStatuses.MinIntervalSeconds;
        }

        if (intervalSeconds > WhatsAppBroadcastStatuses.MaxIntervalSeconds)
        {
            return WhatsAppBroadcastStatuses.MaxIntervalSeconds;
        }

        return intervalSeconds;
    }

    public static string SerializeJids(IEnumerable<string>? jids)
    {
        var normalized = (jids ?? [])
            .Select(jid => (jid ?? string.Empty).Trim())
            .Where(jid => !string.IsNullOrWhiteSpace(jid))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return JsonSerializer.Serialize(normalized);
    }

    public static IReadOnlyList<string> DeserializeJids(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            return items
                .Select(jid => jid.Trim())
                .Where(jid => !string.IsNullOrWhiteSpace(jid))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string ApplyCommissionTag(string? message, string? commissionLink)
    {
        var text = message ?? string.Empty;
        if (!text.Contains(CommissionTag, StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        var link = string.IsNullOrWhiteSpace(commissionLink)
            ? string.Empty
            : commissionLink.Trim();
        return text.Replace(CommissionTag, link, StringComparison.OrdinalIgnoreCase);
    }

    public static bool ContainsHttpUrl(string? message) =>
        !string.IsNullOrWhiteSpace(message) && HttpUrlRegex.IsMatch(message);

    public static string StripHttpUrls(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var stripped = HttpUrlRegex.Replace(message, " ");
        return Regex.Replace(stripped, @"\s{2,}", " ").Trim();
    }

    public static string ComposeDispatchMessage(string? message, string? commissionLink)
    {
        var copy = StripHttpUrls(message);
        var link = (commissionLink ?? string.Empty).Trim();
        copy = ApplyCommissionTag(copy, link);
        if (string.IsNullOrWhiteSpace(link))
        {
            return copy;
        }

        if (copy.Contains(link, StringComparison.OrdinalIgnoreCase))
        {
            return copy;
        }

        if (string.IsNullOrWhiteSpace(copy))
        {
            return link;
        }

        return $"{copy}\n\n{link}";
    }

    public static (string Copy, string Link) SplitDispatchMessage(string? stored)
    {
        var text = (stored ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return (string.Empty, string.Empty);
        }

        var separator = text.LastIndexOf("\n\n", StringComparison.Ordinal);
        if (separator >= 0)
        {
            var maybeLink = text[(separator + 2)..].Trim();
            if (LooksLikeUrl(maybeLink))
            {
                return (text[..separator].Trim(), maybeLink);
            }
        }

        var match = HttpUrlRegex.Match(text);
        if (match.Success && match.Index + match.Length == text.Length)
        {
            return (text[..match.Index].Trim(), match.Value);
        }

        return (text, string.Empty);
    }

    private static bool LooksLikeUrl(string value) =>
        value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    public static DateTime ResolveEditScheduledAt(DateTime scheduledAt, DateTime now, out bool adjusted)
    {
        var localScheduled = scheduledAt.Kind == DateTimeKind.Utc
            ? scheduledAt.ToLocalTime()
            : DateTime.SpecifyKind(scheduledAt, DateTimeKind.Local);
        var localNow = now.Kind == DateTimeKind.Utc ? now.ToLocalTime() : now;
        if (localScheduled > localNow)
        {
            adjusted = false;
            return localScheduled;
        }

        adjusted = true;
        return localNow.AddMinutes(10);
    }

    public static bool IsPrivilegedWhatsAppRole(string? role)
    {
        var value = (role ?? string.Empty).Trim().ToLowerInvariant().Replace("_", string.Empty).Replace("-", string.Empty);
        return value is "admin" or "superadmin" or "superadm";
    }

    public static bool PhoneOrJidMatchesOwner(string? participantId, string? ownerIdentity)
    {
        var left = NormalizeWhatsAppIdentity(participantId);
        var right = NormalizeWhatsAppIdentity(ownerIdentity);
        if (left.Length < 8 || right.Length < 8)
        {
            return false;
        }

        return left.Equals(right, StringComparison.Ordinal)
            || left.EndsWith(right, StringComparison.Ordinal)
            || right.EndsWith(left, StringComparison.Ordinal);
    }

    public static string NormalizeWhatsAppIdentity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var local = value.Trim();
        var at = local.IndexOf('@');
        if (at > 0)
        {
            local = local[..at];
        }

        var digits = new string(local.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("55", StringComparison.Ordinal) && digits.Length > 12)
        {
            return digits;
        }

        return digits;
    }

    public static string ToUiStatus(string? status) =>
        status switch
        {
            WhatsAppBroadcastStatuses.Processing => "Enviando",
            WhatsAppBroadcastStatuses.Completed => "Concluída",
            WhatsAppBroadcastStatuses.Failed => "Falhou",
            WhatsAppBroadcastStatuses.Cancelled => "Cancelado",
            _ => "Agendada"
        };
}

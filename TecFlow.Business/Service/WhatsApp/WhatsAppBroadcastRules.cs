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

    public static string ToUiStatus(string? status) =>
        status switch
        {
            WhatsAppBroadcastStatuses.Processing => "Enviando",
            WhatsAppBroadcastStatuses.Completed => "Concluída",
            WhatsAppBroadcastStatuses.Failed => "Falhou",
            _ => "Agendada"
        };
}

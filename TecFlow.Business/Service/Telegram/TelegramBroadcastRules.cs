using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Entities;

namespace TecFlow.Business.Service.Telegram;

public static class TelegramBroadcastRules
{
    public const string CommissionTag = "[LINK_COMISSAO]";

    public static int ClampIntervalSeconds(int intervalSeconds) =>
        WhatsAppBroadcastRules.ClampIntervalSeconds(intervalSeconds);

    public static string SerializeChatIds(IEnumerable<string>? chatIds) =>
        WhatsAppBroadcastRules.SerializeJids(chatIds);

    public static IReadOnlyList<string> DeserializeChatIds(string? json) =>
        WhatsAppBroadcastRules.DeserializeJids(json);

    public static IReadOnlyList<string> ResolveChatIds(string? json, string? legacyChatId)
    {
        var ids = DeserializeChatIds(json).ToList();
        if (ids.Count > 0)
        {
            return ids;
        }

        var legacy = (legacyChatId ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(legacy) ? [] : [legacy];
    }

    public static string ApplyCommissionTag(string? message, string? commissionLink)
    {
        var text = message ?? string.Empty;
        if (!text.Contains(CommissionTag, StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        var link = string.IsNullOrWhiteSpace(commissionLink) ? string.Empty : commissionLink.Trim();
        return text.Replace(CommissionTag, link, StringComparison.OrdinalIgnoreCase);
    }

    public static string ToUiStatus(string? status) =>
        status switch
        {
            TelegramBroadcastStatuses.Processing => "Enviando",
            TelegramBroadcastStatuses.Completed => "Concluída",
            TelegramBroadcastStatuses.Failed => "Falhou",
            _ => "Agendada"
        };
}

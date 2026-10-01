using TecFlow.Core.Entities;

namespace TecFlow.Business.Service.Telegram;

public static class TelegramBroadcastRules
{
    public const string CommissionTag = "[LINK_COMISSAO]";

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

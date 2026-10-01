using TecFlow.Core.Entities;

namespace TecFlow.Business.Service.WhatsApp;

/// <summary>Regras de instância isolada por UserId e mapeamento de status Evolution → UI.</summary>
public static class WhatsAppSessionRules
{
    public static string BuildInstanceName(int userId) => $"tecflow-u{userId}";

    public static bool TryParseUserId(string? instanceName, out int userId)
    {
        userId = 0;
        var value = (instanceName ?? string.Empty).Trim();
        const string prefix = "tecflow-u";
        if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(value[prefix.Length..], out userId);
    }

    public static string MapFromEvolutionState(string? evolutionState)
    {
        var state = (evolutionState ?? string.Empty).Trim().ToLowerInvariant();
        return state switch
        {
            "open" or "connected" => WhatsAppConnectionStatuses.Connected,
            "connecting" or "qr" or "qrcode" => WhatsAppConnectionStatuses.AwaitingQr,
            _ => WhatsAppConnectionStatuses.Disconnected
        };
    }

    public static string ToUiLabel(string? connectionStatus) =>
        connectionStatus switch
        {
            WhatsAppConnectionStatuses.Connected => "Conectado",
            WhatsAppConnectionStatuses.AwaitingQr => "Aguardando Leitura",
            _ => "Desconectado"
        };

    public static string NormalizeQrDataUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var value = raw.Trim().Trim('"');
        if (value.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        return $"data:image/png;base64,{value}";
    }
}

using TecFlow.Core.Entities;

namespace TecFlow.Business.Service.WhatsApp;

/// <summary>Regras de instância isolada por UserId e mapeamento de status Evolution → UI.</summary>
public static class WhatsAppSessionRules
{
    public const string EvolutionUnreachableMessage =
        "Não foi possível conectar ao servidor da Evolution API no endereço configurado. Verifique se a API está em execução.";

    public static string BuildInstanceName(int userId) => $"tecflow-u{userId}";

    public static string ResolveConnectUiMessage(string? descricao, string? errorMessage, int? statusCode)
    {
        var message = FirstNonEmpty(descricao, errorMessage);
        if (LooksLikeGenericServerError(message, statusCode))
        {
            return EvolutionUnreachableMessage;
        }

        return string.IsNullOrWhiteSpace(message) ? EvolutionUnreachableMessage : message;
    }

    public static bool LooksLikeGenericServerError(string? message, int? statusCode)
    {
        if (statusCode is >= 500)
        {
            return true;
        }

        var text = message ?? string.Empty;
        return text.Contains("500", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Internal Server Error", StringComparison.OrdinalIgnoreCase)
            || text.Contains("erro interno", StringComparison.OrdinalIgnoreCase)
            || text.Contains("erro inesperado ao comunicar", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

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

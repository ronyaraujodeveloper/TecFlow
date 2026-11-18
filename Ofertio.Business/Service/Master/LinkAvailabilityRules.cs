namespace Ofertio.Business.Service.Master;

/// <summary>
/// Disponibilidade pública dos links curtos e das páginas públicas de um inquilino.
/// Pausado, suspenso ou cancelado continua servindo por uma carência; depois o link fica indisponível.
/// Nunca troca o ID de afiliado: ou serve com as credenciais do próprio cliente, ou não serve.
/// </summary>
public static class LinkAvailabilityRules
{
    public const int DefaultGraceDays = 30;
    public const int MaxGraceDays = 365;

    public const string UnavailableLinkTitle = "Este link não está mais ativo";

    public const string UnavailableLinkMessage =
        "O responsável por este link encerrou o uso do serviço. Se você chegou aqui por uma oferta, procure o produto diretamente na loja.";

    public const string UnavailablePageMessage = "Esta página não está mais disponível.";

    public static int NormalizeGraceDays(int? configured) =>
        configured is null or < 0 ? DefaultGraceDays : Math.Min(configured.Value, MaxGraceDays);

    public static int ParseGraceDays(string? raw) =>
        int.TryParse(raw?.Trim(), out var days) ? NormalizeGraceDays(days) : DefaultGraceDays;

    /// <summary>Quando a carência termina; nulo se o estado não bloqueia ou se não há data de referência.</summary>
    public static DateTime? ServingUntil(string? status, DateTime? statusChangedAt, DateTime? canceledAt, int graceDays)
    {
        var normalized = TenantStatuses.Normalize(status);
        if (!TenantLifecycleRules.IsAccessBlocked(normalized))
        {
            return null;
        }

        var since = normalized == TenantStatuses.Cancelado ? canceledAt ?? statusChangedAt : statusChangedAt;
        return since?.AddDays(NormalizeGraceDays(graceDays));
    }

    public static bool IsServing(string? status, DateTime? statusChangedAt, DateTime? canceledAt, DateTime nowUtc, int graceDays)
    {
        if (!TenantLifecycleRules.IsAccessBlocked(status))
        {
            return true;
        }

        var until = ServingUntil(status, statusChangedAt, canceledAt, graceDays);
        return until is null || nowUtc < until.Value;
    }
}
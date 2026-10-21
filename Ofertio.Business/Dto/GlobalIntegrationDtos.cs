namespace Ofertio.Business.Dto;

/// <summary>Estado da conexão Master com uma API oficial (Painel Master ➔ Integrações Globais).</summary>
public class GlobalIntegrationStatusDto
{
    public string Provider { get; set; } = string.Empty;

    public string ProviderLabel { get; set; } = string.Empty;

    /// <summary>Ativo, Expirado ou Desconectado.</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }

    /// <summary>Access token vencido, mas com refresh_token: será renovado na próxima chamada à API.</summary>
    public bool RenewsOnNextCall { get; set; }

    public DateTime? RefreshExpiresAt { get; set; }

    public DateTime? LastRefreshAt { get; set; }

    public string? LastError { get; set; }

    public string? AuthorizedBy { get; set; }

    public DateTime? AuthorizedAt { get; set; }

    public bool HasAppCredentials { get; set; }

    public int SearchCacheMinutes { get; set; }
}

public class GlobalIntegrationSettingsDto
{
    public int SearchCacheMinutes { get; set; }
}

/// <summary>Item do submenu "Integrações Globais" (APIs ativas e previstas).</summary>
public sealed record GlobalIntegrationProviderDto(
    string Key,
    string Label,
    string Icon,
    string? Href,
    bool Available);

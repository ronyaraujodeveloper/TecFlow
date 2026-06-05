namespace TecFlow.Business.Dto;

public class HealthDashboardDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public ComponentHealthDto Database { get; set; } = new();
    public ComponentHealthDto RabbitMq { get; set; } = new();
    public ComponentHealthDto ShopeeApi { get; set; } = new();
    public ComponentHealthDto TikTokShopApi { get; set; } = new();
    public TelemetryMetricsSnapshotDto Metrics { get; set; } = new();
    public List<RecentErrorLogDto> RecentErrors { get; set; } = [];
}

public class ComponentHealthDto
{
    public string Name { get; set; } = string.Empty;
    public HealthStatusLevel Level { get; set; } = HealthStatusLevel.Unknown;
    public string Message { get; set; } = string.Empty;
    public double? ResponseTimeMs { get; set; }
}

public enum HealthStatusLevel
{
    Unknown = 0,
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3
}

public class TelemetryMetricsSnapshotDto
{
    public long ComentariosProcessadosTotal { get; set; }
    public long LinksEnviadosSucessoTotal { get; set; }
    public long ErrosConciliacaoContagem { get; set; }
}

public class RecentErrorLogDto
{
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
}

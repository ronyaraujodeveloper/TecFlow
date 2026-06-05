using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Interfaces.Telemetry;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Messaging;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Health;

public class PlatformHealthService : IPlatformHealthService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ShopeeIntegrationOptions _shopeeOptions;
    private readonly TikTokShopIntegrationOptions _tikTokOptions;
    private readonly RabbitMqOptions _rabbitOptions;
    private readonly ITelemetryRecentErrorRecorder _errorRecorder;
    private readonly ITecFlowBusinessMetrics _metrics;
    private readonly ILogger<PlatformHealthService> _logger;

    public PlatformHealthService(
        AppDbContext db,
        IHttpClientFactory httpClientFactory,
        IOptions<ShopeeIntegrationOptions> shopeeOptions,
        IOptions<TikTokShopIntegrationOptions> tikTokOptions,
        IOptions<RabbitMqOptions> rabbitOptions,
        ITelemetryRecentErrorRecorder errorRecorder,
        ITecFlowBusinessMetrics metrics,
        ILogger<PlatformHealthService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _shopeeOptions = shopeeOptions.Value;
        _tikTokOptions = tikTokOptions.Value;
        _rabbitOptions = rabbitOptions.Value;
        _errorRecorder = errorRecorder;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<HealthDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var dbTask = CheckDatabaseAsync(cancellationToken);
        var rabbitTask = CheckRabbitMqAsync(cancellationToken);
        var shopeeTask = CheckExternalApiAsync("Shopee", _shopeeOptions.ApiBaseUrl, cancellationToken);
        var tiktokTask = CheckExternalApiAsync("TikTok Shop", _tikTokOptions.ApiBaseUrl, cancellationToken);

        await Task.WhenAll(dbTask, rabbitTask, shopeeTask, tiktokTask);

        var recentErrors = _errorRecorder.GetRecent(30)
            .Select(e => new RecentErrorLogDto
            {
                OccurredAtUtc = e.OccurredAtUtc,
                Source = e.Source,
                Message = e.Message,
                Detail = e.Detail
            })
            .ToList();

        var dashboard = new HealthDashboardDto
        {
            Status = true,
            Descricao = "Painel de saúde técnica",
            Database = await dbTask,
            RabbitMq = await rabbitTask,
            ShopeeApi = await shopeeTask,
            TikTokShopApi = await tiktokTask,
            Metrics = new TelemetryMetricsSnapshotDto
            {
                ComentariosProcessadosTotal = _metrics.ComentariosProcessadosTotal,
                LinksEnviadosSucessoTotal = _metrics.LinksEnviadosSucessoTotal,
                ErrosConciliacaoContagem = _metrics.ErrosConciliacaoContagem
            },
            RecentErrors = recentErrors
        };

        if (dashboard.Database.Level == HealthStatusLevel.Unhealthy ||
            dashboard.RabbitMq.Level == HealthStatusLevel.Unhealthy)
        {
            dashboard.Status = false;
            dashboard.Descricao = "Um ou mais componentes críticos indisponíveis.";
        }

        return dashboard;
    }

    private async Task<ComponentHealthDto> CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var ok = await _db.Database.CanConnectAsync(cancellationToken);
            sw.Stop();
            return new ComponentHealthDto
            {
                Name = "PostgreSQL",
                Level = ok ? HealthStatusLevel.Healthy : HealthStatusLevel.Unhealthy,
                Message = ok ? "Conexão ativa." : "Não foi possível conectar.",
                ResponseTimeMs = sw.Elapsed.TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Health check do banco falhou.");
            _errorRecorder.Record("Database", ex.Message);
            return new ComponentHealthDto
            {
                Name = "PostgreSQL",
                Level = HealthStatusLevel.Unhealthy,
                Message = ex.Message,
                ResponseTimeMs = sw.Elapsed.TotalMilliseconds
            };
        }
    }

    private async Task<ComponentHealthDto> CheckRabbitMqAsync(CancellationToken cancellationToken)
    {
        if (!_rabbitOptions.Enabled)
        {
            return new ComponentHealthDto
            {
                Name = "RabbitMQ",
                Level = HealthStatusLevel.Degraded,
                Message = "Mensageria desabilitada (RabbitMq:Enabled=false)."
            };
        }

        var sw = Stopwatch.StartNew();
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_rabbitOptions.Host, _rabbitOptions.Port, cancellationToken);
            sw.Stop();
            return new ComponentHealthDto
            {
                Name = "RabbitMQ",
                Level = HealthStatusLevel.Healthy,
                Message = $"Broker acessível em {_rabbitOptions.Host}:{_rabbitOptions.Port}.",
                ResponseTimeMs = sw.Elapsed.TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _errorRecorder.Record("RabbitMQ", ex.Message);
            return new ComponentHealthDto
            {
                Name = "RabbitMQ",
                Level = HealthStatusLevel.Unhealthy,
                Message = ex.Message,
                ResponseTimeMs = sw.Elapsed.TotalMilliseconds
            };
        }
    }

    private async Task<ComponentHealthDto> CheckExternalApiAsync(
        string name,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new ComponentHealthDto
            {
                Name = name,
                Level = HealthStatusLevel.Degraded,
                Message = "URL base não configurada."
            };
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(8);
            using var request = new HttpRequestMessage(HttpMethod.Head, baseUrl);
            using var response = await client.SendAsync(request, cancellationToken);
            sw.Stop();

            var level = response.IsSuccessStatusCode || (int)response.StatusCode == 405
                ? HealthStatusLevel.Healthy
                : HealthStatusLevel.Degraded;

            return new ComponentHealthDto
            {
                Name = name,
                Level = level,
                Message = $"HTTP {(int)response.StatusCode} — {baseUrl}",
                ResponseTimeMs = sw.Elapsed.TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _errorRecorder.Record(name, ex.Message);
            return new ComponentHealthDto
            {
                Name = name,
                Level = HealthStatusLevel.Unhealthy,
                Message = ex.Message,
                ResponseTimeMs = sw.Elapsed.TotalMilliseconds
            };
        }
    }
}

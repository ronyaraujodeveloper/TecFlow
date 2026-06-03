using Microsoft.Extensions.Logging;

namespace TecFlow.Business.Service.Application;

public class MetricsApplicationService
{
    private readonly ILogger<MetricsApplicationService> _logger;

    public MetricsApplicationService(ILogger<MetricsApplicationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IEnumerable<string>> GetMetricsAsync()
    {
        var metrics = new List<string>
        {
            "Vendas: 10000",
            "Engajamento: 5200",
            "TaxaDeConversao: 2.7%"
        };

        _logger.LogInformation("Geradas {Count} métricas.", metrics.Count);
        return Task.FromResult<IEnumerable<string>>(metrics);
    }
}

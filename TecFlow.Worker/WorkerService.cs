using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;

public class WorkerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkerService> _logger;

    public WorkerService(IServiceScopeFactory scopeFactory, ILogger<WorkerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Iniciando ciclo de automação: {time}", DateTimeOffset.Now);

            using (var scope = _scopeFactory.CreateScope())
            {
                var orquestrador = scope.ServiceProvider.GetRequiredService<IOrquestradorService>();
                await orquestrador.ExecuteFullPipelineAsync();
            }

            _logger.LogInformation("Ciclo concluído. Dormindo por 1 hora.");
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
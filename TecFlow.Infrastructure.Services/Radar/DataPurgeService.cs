using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class DataPurgeService : IDataPurgeService
{
    private readonly AppDbContext _context;
    private readonly IProductImageCleanupService _mediaCleanup;
    private readonly ColdStorageArchiver _archiver;
    private readonly ILogger<DataPurgeService> _logger;

    public DataPurgeService(
        AppDbContext context,
        IProductImageCleanupService mediaCleanup,
        ColdStorageArchiver archiver,
        ILogger<DataPurgeService> logger)
    {
        _context = context;
        _mediaCleanup = mediaCleanup;
        _archiver = archiver;
        _logger = logger;
    }

    public async Task<DataPurgeResult> RunCycleAsync(CancellationToken cancellationToken = default)
    {
        var media = 0;
        for (var round = 0; round < DataPurgeRules.MaxDeleteRounds; round++)
        {
            var batch = await _mediaCleanup.PurgeExpiredAsync(cancellationToken);
            media += batch;
            if (batch == 0)
            {
                break;
            }
        }

        var archived = 0;
        var deleted = 0;
        for (var round = 0; round < DataPurgeRules.MaxDeleteRounds; round++)
        {
            archived += await _archiver.ArchiveDueMessagesAsync(cancellationToken);
            var roundDelete = await _context.Database.ExecuteSqlRawAsync(
                """
                DELETE TOP (5000) FROM GroupCapturedMessages
                WHERE CreatedAt < DATEADD(day, -14, GETUTCDATE())
                """,
                cancellationToken);
            deleted += roundDelete;
            if (roundDelete == 0)
            {
                break;
            }
        }

        var reorganized = false;
        if (deleted > 0)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "ALTER INDEX ALL ON GroupCapturedMessages REORGANIZE",
                    cancellationToken);
                reorganized = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reorganização de índices de GroupCapturedMessages falhou.");
            }
        }

        _logger.LogInformation(
            "Data purge concluído. Midia={Media} Historico={Archive} Mensagens={Deleted} Indices={Indexes}",
            media,
            archived,
            deleted,
            reorganized);
        return new DataPurgeResult
        {
            MediaPurged = media,
            PricesArchived = archived,
            MessagesDeleted = deleted,
            IndexesReorganized = reorganized
        };
    }
}

public sealed class DataPurgeHost
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataPurgeHost> _logger;

    public DataPurgeHost(IServiceScopeFactory scopeFactory, ILogger<DataPurgeHost> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RunForeverAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var purge = scope.ServiceProvider.GetRequiredService<IDataPurgeService>();
                await purge.RunCycleAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Worker de expurgo de dados falhou.");
            }

            try
            {
                await Task.Delay(DataPurgeRules.DelayUntilNextDailyUtc(DateTime.UtcNow), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}

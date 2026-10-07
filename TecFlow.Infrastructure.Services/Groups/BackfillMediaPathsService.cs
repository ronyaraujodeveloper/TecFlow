using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class BackfillMediaPathsService : IProductImageUrlLinkBackfillService
{
    private readonly IGroupCapturedMessagesService _capturedMessages;
    private readonly ILogger<BackfillMediaPathsService> _logger;

    public BackfillMediaPathsService(
        IGroupCapturedMessagesService capturedMessages,
        ILogger<BackfillMediaPathsService> logger)
    {
        _capturedMessages = capturedMessages;
        _logger = logger;
    }

    public async Task<int> LinkExistingFilesOnceAsync(CancellationToken cancellationToken = default)
    {
        var linked = await _capturedMessages.LinkExistingDownloadedImagesAsync(cancellationToken);
        if (linked > 0)
        {
            _logger.LogInformation("Backfill de ImageUrl a partir do disco. Vinculadas={Linked}", linked);
        }

        return linked;
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class ProductImageUrlLinkBackfillService : IProductImageUrlLinkBackfillService
{
    public const string MarkerFileName = ".product-imageurl-link.done";

    private readonly IGroupCapturedMessagesService _capturedMessages;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ProductImageUrlLinkBackfillService> _logger;

    public ProductImageUrlLinkBackfillService(
        IGroupCapturedMessagesService capturedMessages,
        IWebHostEnvironment environment,
        ILogger<ProductImageUrlLinkBackfillService> logger)
    {
        _capturedMessages = capturedMessages;
        _environment = environment;
        _logger = logger;
    }

    public async Task<int> LinkExistingFilesOnceAsync(CancellationToken cancellationToken = default)
    {
        var webRoot = ProductImageStorageRules.ResolveWebRoot(
            _environment.WebRootPath,
            _environment.ContentRootPath);
        var uploads = Path.Combine(webRoot, "uploads", "products");
        Directory.CreateDirectory(uploads);
        var marker = Path.Combine(uploads, MarkerFileName);
        if (File.Exists(marker))
        {
            return 0;
        }

        var photos = ProductImageStorageRules.EnumerateExistingPhotos(webRoot)
            .GroupBy(item => (item.TenantId, item.MessageId))
            .Select(group => group.OrderByDescending(item => item.LastWriteUtc).First())
            .ToList();

        var linked = 0;
        foreach (var photo in photos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            linked += await _capturedMessages.UpdateImageUrlAsync(
                photo.MessageId,
                photo.WebRelativeUrl,
                photo.TenantId,
                cancellationToken);
        }

        await File.WriteAllTextAsync(marker, DateTime.UtcNow.ToString("O"), cancellationToken);
        _logger.LogInformation(
            "Backfill único de ImageUrl a partir do disco concluído. Arquivos={Files} Linhas={Rows}",
            photos.Count,
            linked);
        return linked;
    }
}

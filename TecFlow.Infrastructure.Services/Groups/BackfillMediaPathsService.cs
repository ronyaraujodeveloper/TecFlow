using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class BackfillMediaPathsService : IProductImageUrlLinkBackfillService
{
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<BackfillMediaPathsService> _logger;

    public BackfillMediaPathsService(
        AppDbContext dbContext,
        IWebHostEnvironment environment,
        ILogger<BackfillMediaPathsService> logger)
    {
        _dbContext = dbContext;
        _environment = environment;
        _logger = logger;
    }

    public async Task<int> LinkExistingFilesOnceAsync(CancellationToken cancellationToken = default)
    {
        var pendingMessages = await _dbContext.GroupCapturedMessages
            .Where(item => (item.ProductImageUrl == null || item.ProductImageUrl == "")
                && item.ExternalMessageId != null
                && item.ExternalMessageId != "")
            .ToListAsync(cancellationToken);
        if (pendingMessages.Count == 0)
        {
            return 0;
        }

        var webRoot = ProductImageStorageRules.ResolveWebRoot(
            _environment.WebRootPath,
            _environment.ContentRootPath,
            AppDomain.CurrentDomain.BaseDirectory);
        Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "products"));

        var linked = 0;
        foreach (var msg in pendingMessages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = ProductImageStorageRules.TryFindPhotoForMessageId(webRoot, msg.ExternalMessageId);
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                continue;
            }

            msg.ProductImageUrl = relativePath;
            msg.MediaUrl = relativePath;
            msg.Touch();
            linked++;
        }

        if (linked > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Backfill de ImageUrl a partir do disco. Pendentes={Pending} Vinculadas={Linked} WebRoot={WebRoot}",
            pendingMessages.Count,
            linked,
            webRoot);
        return linked;
    }
}

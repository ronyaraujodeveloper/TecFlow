using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class ProductImageCleanupService : IProductImageCleanupService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ProductImageCleanupService> _logger;

    public ProductImageCleanupService(
        AppDbContext context,
        IWebHostEnvironment environment,
        ILogger<ProductImageCleanupService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(ProductImageStorageRules.Retention);
        var items = await _context.GroupCapturedMessages
            .Where(item => item.CreatedAt <= cutoff
                && item.ProductImageUrl != null
                && item.ProductImageUrl.Contains("/uploads/products"))
            .OrderBy(item => item.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;
        var purged = 0;
        foreach (var item in items)
        {
            if (!ProductImageStorageRules.IsLocalProductImage(item.ProductImageUrl)
                && !ProductImageStorageRules.IsLocalProductImage(item.MediaUrl))
            {
                continue;
            }

            TryDeleteFile(webRoot, item.ProductImageUrl);
            if (ProductImageStorageRules.IsLocalProductImage(item.MediaUrl))
            {
                TryDeleteFile(webRoot, item.MediaUrl);
                item.MediaUrl = null;
            }

            item.ProductImageUrl = null;
            item.Touch();
            purged++;
        }

        if (purged > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Expurgo de imagens de produto. Arquivos={Count} Corte={Cutoff:u}", purged, cutoff);
        return purged;
    }

    private void TryDeleteFile(string webRoot, string? storedUrl)
    {
        var path = ProductImageStorageRules.TryResolvePhysicalPath(webRoot, storedUrl);
        if (path is null || !File.Exists(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao apagar imagem antiga. Path={Path}", path);
        }
    }
}

public sealed class ProductImageCleanupHost
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductImageCleanupHost> _logger;

    public ProductImageCleanupHost(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductImageCleanupHost> logger)
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
                var cleanup = scope.ServiceProvider.GetRequiredService<IProductImageCleanupService>();
                await cleanup.PurgeExpiredAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Worker de expurgo de imagens falhou.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromDays(1), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class OfferProductMediaStore : IOfferProductMediaStore
{
    public const string RelativeUploadsFolder = "uploads/products";

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<OfferProductMediaStore> _logger;

    public OfferProductMediaStore(
        IWebHostEnvironment environment,
        ILogger<OfferProductMediaStore> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string?> SaveProductPhotoAsync(
        int userId,
        string? messageId,
        byte[] photoBytes,
        CancellationToken cancellationToken = default)
    {
        if (photoBytes is not { Length: > 0 })
        {
            return null;
        }

        try
        {
            var webRoot = ResolveWebRoot();
            var utcNow = DateTime.UtcNow;
            var tenantId = userId;
            var folder = ProductImageStorageRules.BuildPhysicalFolder(webRoot, tenantId, utcNow);
            Directory.CreateDirectory(folder);

            var fileName = ProductImageStorageRules.BuildFileName(messageId);
            var fullPath = Path.Combine(folder, fileName);
            await File.WriteAllBytesAsync(fullPath, photoBytes, cancellationToken);
            var relativePath = ProductImageStorageRules.BuildRelativeUrl(tenantId, utcNow, fileName);
            return ProductImageStorageRules.ToWebRelativePath(relativePath.Replace('\\', '/'));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao gravar foto da oferta. UserId={UserId}", userId);
            return null;
        }
    }

    private string ResolveWebRoot() =>
        string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;
}

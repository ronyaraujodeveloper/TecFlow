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

    public Task<string?> SaveProductPhotoAsync(
        int userId,
        string? messageId,
        byte[] photoBytes,
        CancellationToken cancellationToken = default)
    {
        if (photoBytes is not { Length: > 0 })
        {
            return Task.FromResult<string?>(null);
        }

        return SaveFromStreamAsync(
            userId,
            messageId,
            async (stream, token) => await stream.WriteAsync(photoBytes, token),
            cancellationToken);
    }

    public async Task<string?> SaveFromStreamAsync(
        int tenantId,
        string? messageId,
        Func<Stream, CancellationToken, Task> writeAsync,
        CancellationToken cancellationToken = default)
    {
        if (writeAsync is null)
        {
            return null;
        }

        try
        {
            var webRoot = ProductImageStorageRules.ResolveWebRoot(
                _environment.WebRootPath,
                _environment.ContentRootPath);
            var fileName = ProductImageStorageRules.BuildFileName(messageId);
            var (absoluteDir, absoluteFilePath, webRelativeUrl) = ProductImageStorageRules.BuildSaveTarget(
                webRoot,
                tenantId,
                fileName,
                DateTime.UtcNow);
            Directory.CreateDirectory(absoluteDir);

            await using (var stream = File.Create(absoluteFilePath))
            {
                await writeAsync(stream, cancellationToken);
            }

            var info = new FileInfo(absoluteFilePath);
            if (!info.Exists || info.Length <= 0)
            {
                if (info.Exists)
                {
                    File.Delete(absoluteFilePath);
                }

                return null;
            }

            _logger.LogInformation("Imagem salva no caminho: {path}", absoluteFilePath);
            return webRelativeUrl;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao gravar foto da oferta. UserId={UserId}", tenantId);
            return null;
        }
    }

    public bool ExistsOnDisk(string? webRelativeUrl)
    {
        var webRoot = ProductImageStorageRules.ResolveWebRoot(
            _environment.WebRootPath,
            _environment.ContentRootPath);
        return ProductImageStorageRules.FileExistsOnDisk(webRoot, webRelativeUrl);
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class OfferProductMediaStore : IOfferProductMediaStore
{
    public const string RelativeUploadsFolder = "uploads/products";

    private readonly IWebHostEnvironment _environment;
    private readonly IImageOptimizationService _optimizer;
    private readonly ILogger<OfferProductMediaStore> _logger;

    public OfferProductMediaStore(
        IWebHostEnvironment environment,
        IImageOptimizationService optimizer,
        ILogger<OfferProductMediaStore> logger)
    {
        _environment = environment;
        _optimizer = optimizer;
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
        if (writeAsync is null || tenantId <= 0)
        {
            return null;
        }

        try
        {
            await using var raw = new MemoryStream();
            await writeAsync(raw, cancellationToken);
            if (raw.Length <= 0)
            {
                return null;
            }

            raw.Position = 0;
            var digits = string.IsNullOrWhiteSpace(messageId)
                ? string.Empty
                : new string(messageId.Where(char.IsDigit).ToArray());
            _ = long.TryParse(digits, out var telegramId);
            return await _optimizer.ProcessAndSaveImageAsync(
                raw,
                tenantId.ToString(),
                telegramId,
                cancellationToken);
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

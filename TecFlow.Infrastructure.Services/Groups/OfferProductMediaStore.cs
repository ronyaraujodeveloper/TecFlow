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

        await using var raw = new MemoryStream();
        try
        {
            await writeAsync(raw, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Download da foto da oferta falhou; a mensagem fica sem imagem. Tenant={TenantId} MessageId={MessageId}",
                tenantId,
                messageId);
            return null;
        }

        if (raw.Length <= 0)
        {
            _logger.LogWarning(
                "Download da foto da oferta retornou 0 bytes; a mensagem fica sem imagem. Tenant={TenantId} MessageId={MessageId}",
                tenantId,
                messageId);
            return null;
        }

        string? relativePath;
        try
        {
            raw.Position = 0;
            var digits = string.IsNullOrWhiteSpace(messageId)
                ? string.Empty
                : new string(messageId.Where(char.IsDigit).ToArray());
            _ = long.TryParse(digits, out var telegramId);
            relativePath = await _optimizer.ProcessAndSaveImageAsync(
                raw,
                tenantId.ToString(),
                telegramId,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Conversão WebP da foto da oferta falhou; a mensagem fica sem imagem. Tenant={TenantId} MessageId={MessageId} Bytes={Bytes}",
                tenantId,
                messageId,
                raw.Length);
            return null;
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            _logger.LogWarning(
                "Conversão WebP da foto da oferta não gerou arquivo; a mensagem fica sem imagem. Tenant={TenantId} MessageId={MessageId} Bytes={Bytes}",
                tenantId,
                messageId,
                raw.Length);
            return null;
        }

        var webRoot = ProductImageStorageRules.ResolveWebRoot(_environment.WebRootPath, _environment.ContentRootPath);
        var physicalPath = ProductImageStorageRules.TryResolvePhysicalPath(webRoot, relativePath);
        if (physicalPath is null || !File.Exists(physicalPath))
        {
            _logger.LogWarning(
                "Foto da oferta não encontrada no disco após a gravação; URL descartada. Tenant={TenantId} MessageId={MessageId} Url={Url} Path={Path}",
                tenantId,
                messageId,
                relativePath,
                physicalPath);
            return null;
        }

        return relativePath;
    }

    public bool ExistsOnDisk(string? webRelativeUrl)
    {
        var webRoot = ProductImageStorageRules.ResolveWebRoot(
            _environment.WebRootPath,
            _environment.ContentRootPath);
        return ProductImageStorageRules.FileExistsOnDisk(webRoot, webRelativeUrl);
    }
}

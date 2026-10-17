using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class ImageOptimizationService : IImageOptimizationService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ImageOptimizationService> _logger;

    public ImageOptimizationService(
        IWebHostEnvironment environment,
        ILogger<ImageOptimizationService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public Task<string?> ProcessAndSaveImageAsync(
        Stream rawStream,
        string tenantId,
        long messageId,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(tenantId, out var tenant) || tenant <= 0 || rawStream is null)
        {
            return Task.FromResult<string?>(null);
        }

        var webRoot = ProductImageStorageRules.ResolveWebRoot(
            _environment.WebRootPath,
            _environment.ContentRootPath);
        var fileName = ImageOptimizationRules.BuildOutputFileName(messageId);
        var (_, absolutePath, webRelative) = ProductImageStorageRules.BuildSaveTarget(
            webRoot,
            tenant,
            fileName,
            DateTime.UtcNow);
        return ProcessAndSaveToAbsoluteAsync(rawStream, absolutePath, webRelative, cancellationToken);
    }

    public Task<string?> ProcessAndSaveToRelativePathAsync(
        Stream rawStream,
        string webRelativePathWithoutExtension,
        CancellationToken cancellationToken = default)
    {
        var relative = (webRelativePathWithoutExtension ?? string.Empty).Replace('\\', '/').Trim();
        if (!relative.StartsWith('/'))
        {
            relative = "/" + relative;
        }

        relative = Path.ChangeExtension(relative, ImageOptimizationRules.OutputExtension) ?? relative;
        if (!StrictImageIngestionPolicy.AllowsWrite(relative))
        {
            return Task.FromResult<string?>(null);
        }

        var webRoot = ProductImageStorageRules.ResolveWebRoot(
            _environment.WebRootPath,
            _environment.ContentRootPath);
        var combined = Path.GetFullPath(Path.Combine(
            webRoot,
            relative.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        var uploadsRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads"));
        if (!combined.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<string?>(null);
        }

        return ProcessAndSaveToAbsoluteAsync(rawStream, combined, relative, cancellationToken);
    }

    private async Task<string?> ProcessAndSaveToAbsoluteAsync(
        Stream rawStream,
        string absolutePath,
        string webRelative,
        CancellationToken cancellationToken)
    {
        var tempPath = $"{absolutePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Diretório de mídia criado: {Directory}", directory);
            }

            rawStream.Position = 0;
            using var image = await Image.LoadAsync<Rgba32>(rawStream, cancellationToken);
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(ImageOptimizationRules.MaxEdgePx, ImageOptimizationRules.MaxEdgePx),
                Mode = ResizeMode.Max
            }));

            if (ImageOptimizationRules.NeedsNeutralCanvas(image.Width, image.Height))
            {
                var side = ImageOptimizationRules.CanvasSide(image.Width, image.Height);
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(side, side),
                    Mode = ResizeMode.BoxPad,
                    Position = AnchorPositionMode.Center,
                    PadColor = Color.ParseHex(ImageOptimizationRules.NeutralCanvasHex)
                }));
            }

            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.XmpProfile = null;
            image.Metadata.IptcProfile = null;

            var quality = ImageOptimizationRules.MaxQuality;
            var savedPath = await EncodeAsync(image, absolutePath, tempPath, quality, cancellationToken);
            var next = ImageOptimizationRules.ChooseQuality(new FileInfo(savedPath).Length, quality);
            if (next != quality)
            {
                savedPath = await EncodeAsync(image, absolutePath, tempPath, next, cancellationToken);
            }

            var saved = new FileInfo(savedPath);
            if (!File.Exists(savedPath) || saved.Length <= 0)
            {
                _logger.LogWarning(
                    "Imagem não confirmada no disco após a conversão; descartada. Path={Path} Bytes={Bytes}",
                    savedPath,
                    saved.Exists ? saved.Length : 0);
                TryDelete(savedPath);
                return null;
            }

            var relative = string.Equals(savedPath, absolutePath, StringComparison.OrdinalIgnoreCase)
                ? webRelative
                : Path.ChangeExtension(webRelative, Path.GetExtension(savedPath));
            _logger.LogInformation(
                "Imagem otimizada salva no caminho: {path} ({Bytes} bytes, qualidade {Quality})",
                savedPath,
                saved.Length,
                next);
            return ProductImageStorageRules.EnsureLeadingSlash(relative);
        }
        catch (UnknownImageFormatException ex)
        {
            _logger.LogWarning(ex, "Formato de imagem rejeitado pela regra estrita de ingestão. Path={Path}", absolutePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao converter/gravar imagem em WebP. Path={Path}", absolutePath);
            return null;
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    /// <summary>Codifica em arquivo temporário e só publica o nome final depois do flush físico, para nunca expor arquivo parcial.</summary>
    private static async Task<string> EncodeAsync(
        Image<Rgba32> image,
        string absolutePath,
        string tempPath,
        int quality,
        CancellationToken cancellationToken)
    {
        var targetPath = absolutePath;
        await using (var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            try
            {
                await image.SaveAsWebpAsync(
                    output,
                    new WebpEncoder { Quality = quality, FileFormat = WebpFileFormatType.Lossy },
                    cancellationToken);
            }
            catch (NotSupportedException)
            {
                output.SetLength(0);
                output.Position = 0;
                await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = quality }, cancellationToken);
                targetPath = Path.ChangeExtension(absolutePath, ".jpg");
            }

            output.Flush(flushToDisk: true);
        }

        File.Move(tempPath, targetPath, overwrite: true);
        return targetPath;
    }

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Não foi possível remover arquivo de mídia incompleto. Path={Path}", path);
        }
    }
}

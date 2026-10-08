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
        try
        {
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

            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var quality = ImageOptimizationRules.MaxQuality;
            await EncodeAsync(image, absolutePath, quality, cancellationToken);
            var length = new FileInfo(absolutePath).Length;
            var next = ImageOptimizationRules.ChooseQuality(length, quality);
            if (next != quality)
            {
                await EncodeAsync(image, absolutePath, next, cancellationToken);
            }

            var saved = new FileInfo(absolutePath);
            if (!saved.Exists || saved.Length <= 0)
            {
                return null;
            }

            _logger.LogInformation("Imagem otimizada salva no caminho: {path}", absolutePath);
            return ProductImageStorageRules.EnsureLeadingSlash(webRelative);
        }
        catch (UnknownImageFormatException ex)
        {
            _logger.LogWarning(ex, "Formato de imagem rejeitado pela regra estrita de ingestão.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao otimizar imagem. Path={Path}", absolutePath);
            return null;
        }
    }

    private static async Task EncodeAsync(
        Image<Rgba32> image,
        string absolutePath,
        int quality,
        CancellationToken cancellationToken)
    {
        await using var output = File.Create(absolutePath);
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
            var jpegPath = Path.ChangeExtension(absolutePath, ".jpg");
            await using var jpeg = File.Create(jpegPath);
            await image.SaveAsJpegAsync(jpeg, new JpegEncoder { Quality = quality }, cancellationToken);
        }
    }
}

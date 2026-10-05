using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Configuration;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Util.Text;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class OfferProductMediaStore : IOfferProductMediaStore
{
    public const string RelativeUploadsFolder = "uploads/products";

    private readonly IWebHostEnvironment _environment;
    private readonly ShortLinkOptions _shortLinks;
    private readonly ILogger<OfferProductMediaStore> _logger;

    public OfferProductMediaStore(
        IWebHostEnvironment environment,
        IOptions<ShortLinkOptions> shortLinks,
        ILogger<OfferProductMediaStore> logger)
    {
        _environment = environment;
        _shortLinks = shortLinks.Value;
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
            var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;
            var folder = Path.Combine(webRoot, "uploads", "products");
            Directory.CreateDirectory(folder);

            var extension = photoBytes.Length >= 8
                && photoBytes[0] == 0x89
                && photoBytes[1] == 0x50
                    ? ".png"
                    : ".jpg";
            var safeMessage = string.IsNullOrWhiteSpace(messageId)
                ? "msg"
                : new string(messageId.Where(char.IsLetterOrDigit).ToArray());
            var fileName = $"{userId}-{safeMessage}-{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, fileName);
            await File.WriteAllBytesAsync(fullPath, photoBytes, cancellationToken);

            var publicHost = ShortLinkPublicUrl.NormalizeHost(_shortLinks.PublicBaseUrl);
            return $"{publicHost}/{RelativeUploadsFolder}/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao gravar foto da oferta. UserId={UserId}", userId);
            return null;
        }
    }
}

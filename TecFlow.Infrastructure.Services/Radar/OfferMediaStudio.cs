using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TecFlow.Business.Configuration;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Util.Text;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class OfferMediaStudio : IOfferMediaStudio
{
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ShortLinkOptions _shortLinks;
    private readonly ILogger<OfferMediaStudio> _logger;

    public OfferMediaStudio(
        IWebHostEnvironment environment,
        IHttpClientFactory httpFactory,
        IOptions<ShortLinkOptions> shortLinks,
        ILogger<OfferMediaStudio> logger)
    {
        _environment = environment;
        _httpFactory = httpFactory;
        _shortLinks = shortLinks.Value;
        _logger = logger;
    }

    public async Task<OfferMediaResponseDto> ApplyFrameAsync(
        int userId,
        OfferMediaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var source = request.ImageUrl;
        if (string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(request.PageUrl))
        {
            var html = await DownloadTextAsync(request.PageUrl, cancellationToken);
            source = ProductMetadataHtmlParser.Parse(html, request.PageUrl).ProductImageUrl;
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            return new OfferMediaResponseDto { Status = false, Descricao = "Informe a URL da imagem ou da página do produto." };
        }

        try
        {
            var bytes = await DownloadBytesAsync(source, cancellationToken);
            using var image = Image.Load<Rgba32>(bytes);
            var pad = Math.Max(36, image.Height / 12);
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(image.Width, image.Height + pad),
                Mode = ResizeMode.BoxPad,
                Position = AnchorPositionMode.Top,
                PadColor = Color.ParseHex("E85D04")
            }));
            var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;
            var folder = Path.Combine(webRoot, "uploads", "frames");
            Directory.CreateDirectory(folder);
            var fileName = $"{userId}-{Guid.NewGuid():N}.jpg";
            var path = Path.Combine(folder, fileName);
            await image.SaveAsJpegAsync(path, cancellationToken);
            return new OfferMediaResponseDto
            {
                Status = true,
                Descricao = "Moldura promocional aplicada.",
                FramedImageUrl = $"{ShortLinkPublicUrl.NormalizeHost(_shortLinks.PublicBaseUrl)}/uploads/frames/{fileName}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao aplicar moldura. Url={Url}", source);
            return new OfferMediaResponseDto { Status = false, Descricao = "Não foi possível aplicar a moldura nesta imagem." };
        }
    }

    public async Task<OfferMediaResponseDto> ExtractVideoAsync(
        OfferMediaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var pageUrl = request.PageUrl ?? request.ImageUrl;
        if (string.IsNullOrWhiteSpace(pageUrl))
        {
            return new OfferMediaResponseDto { Status = false, Descricao = "Informe a URL da página do produto." };
        }

        try
        {
            var html = await DownloadTextAsync(pageUrl, cancellationToken);
            var video = FirstVideo(html);
            if (string.IsNullOrWhiteSpace(video))
            {
                return new OfferMediaResponseDto { Status = false, Descricao = "Nenhum vídeo do produto encontrado na página." };
            }

            var bytes = await DownloadBytesAsync(video, cancellationToken);
            var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;
            var folder = Path.Combine(webRoot, "uploads", "videos");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid():N}.mp4";
            await File.WriteAllBytesAsync(Path.Combine(folder, fileName), bytes, cancellationToken);
            return new OfferMediaResponseDto
            {
                Status = true,
                Descricao = "Vídeo do produto baixado sem marca d'água da página.",
                VideoUrl = $"{ShortLinkPublicUrl.NormalizeHost(_shortLinks.PublicBaseUrl)}/uploads/videos/{fileName}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao baixar vídeo. Url={Url}", pageUrl);
            return new OfferMediaResponseDto { Status = false, Descricao = "Não foi possível baixar o vídeo do produto." };
        }
    }

    private async Task<string> DownloadTextAsync(string url, CancellationToken cancellationToken)
    {
        var client = _httpFactory.CreateClient();
        using var response = await client.GetAsync(url, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task<byte[]> DownloadBytesAsync(string url, CancellationToken cancellationToken)
    {
        var client = _httpFactory.CreateClient();
        return await client.GetByteArrayAsync(url, cancellationToken);
    }

    private static string? FirstVideo(string html)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            html ?? string.Empty,
            @"<meta[^>]+(?:property|name)=[""'](?:og:video|og:video:url|og:video:secure_url|twitter:player:stream)[""'][^>]+content=[""'](?<url>[^""']+)[""']",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups["url"].Value.Trim();
        }

        match = System.Text.RegularExpressions.Regex.Match(
            html ?? string.Empty,
            @"content=[""'](?<url>https?://[^""']+\.(?:mp4|webm))[""']",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["url"].Value.Trim() : null;
    }
}

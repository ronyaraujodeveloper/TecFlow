using System.Net;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Common;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;

namespace TecFlow.Infrastructure.Services.LinkStrategies;

/// <summary>Expande encurtadores e extrai OpenGraph/JSON-LD da página do produto.</summary>
public sealed class ProductMetadataService : IProductMetadataService
{
    private const int MaxHtmlChars = 512_000;

    private static readonly string[] InvalidTitles =
    [
        "opaanlp",
        "nsbo",
        "shopee",
        "shopee brasil",
        "verification",
        "captcha",
        "just a moment"
    ];

    private readonly IUrlExpansionService _urlExpansionService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProductMetadataService> _logger;

    public ProductMetadataService(
        IUrlExpansionService urlExpansionService,
        IHttpClientFactory httpClientFactory,
        ILogger<ProductMetadataService> logger)
    {
        _urlExpansionService = urlExpansionService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ProductMetadataDto> ExtractAsync(
        string productUrl,
        CancellationToken cancellationToken = default)
    {
        var workingUrl = string.IsNullOrWhiteSpace(productUrl) ? string.Empty : productUrl.Trim();
        var resolvedUrl = workingUrl;

        try
        {
            if (!string.IsNullOrWhiteSpace(workingUrl))
            {
                resolvedUrl = await _urlExpansionService.ExpandUrlAsync(workingUrl, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Falha ao expandir URL para metadados do produto. Url={Url}",
                workingUrl);
            resolvedUrl = workingUrl;
        }

        resolvedUrl = UnwrapNestedDestinationUrl(resolvedUrl);

        try
        {
            if (string.IsNullOrWhiteSpace(resolvedUrl) || !Uri.TryCreate(resolvedUrl, UriKind.Absolute, out _))
            {
                return SanitizeMetadata(ProductMetadataHtmlParser.FromUrlFallback(workingUrl), workingUrl);
            }

            var client = _httpClientFactory.CreateClient(IntegrationHttpClientNames.ProductMetadata);
            using var request = new HttpRequestMessage(HttpMethod.Get, resolvedUrl);
            ApplyAntiBotBrowserHeaders(request);
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Scraping de metadados recusado. Status={Status} Url={Url}",
                    (int)response.StatusCode,
                    resolvedUrl);
                return SanitizeMetadata(ProductMetadataHtmlParser.FromUrlFallback(resolvedUrl), resolvedUrl);
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            if (html.Length > MaxHtmlChars)
            {
                html = html[..MaxHtmlChars];
            }

            var parsed = ProductMetadataHtmlParser.Parse(html, resolvedUrl);
            return SanitizeMetadata(parsed, resolvedUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Falha ao extrair metadados do produto. Url={Url}",
                resolvedUrl);
            return SanitizeMetadata(ProductMetadataHtmlParser.FromUrlFallback(resolvedUrl), resolvedUrl);
        }
    }

    public static string UnwrapNestedDestinationUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return url ?? string.Empty;
        }

        var query = uri.Query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(query))
        {
            return uri.ToString();
        }

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            var key = HttpUtilityUrlDecode(parts[0]);
            if (!key.Equals("target", StringComparison.OrdinalIgnoreCase)
                && !key.Equals("redirect", StringComparison.OrdinalIgnoreCase)
                && !key.Equals("redirect_url", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var nested = HttpUtilityUrlDecode(parts[1]);
            if (Uri.TryCreate(nested, UriKind.Absolute, out var nestedUri)
                && (nestedUri.Scheme == Uri.UriSchemeHttp || nestedUri.Scheme == Uri.UriSchemeHttps))
            {
                return UnwrapNestedDestinationUrl(nestedUri.ToString());
            }
        }

        return uri.ToString();
    }

    public static bool IsInvalidProductName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var trimmed = name.Trim();
        if (trimmed.All(char.IsDigit))
        {
            return true;
        }

        if (!trimmed.Contains(' ', StringComparison.Ordinal)
            && trimmed.Length <= 24
            && trimmed.All(char.IsLetterOrDigit))
        {
            return true;
        }

        var normalized = trimmed.ToLowerInvariant();
        foreach (var invalid in InvalidTitles)
        {
            if (normalized.Equals(invalid, StringComparison.Ordinal)
                || normalized.Contains(invalid, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static ProductMetadataDto SanitizeMetadata(ProductMetadataDto parsed, string resolvedUrl)
    {
        ApplyExpandedUrlName(parsed, resolvedUrl);
        if (!IsInvalidProductName(parsed.ProductName))
        {
            return parsed;
        }

        var unwrapped = UnwrapNestedDestinationUrl(resolvedUrl);
        ApplyExpandedUrlName(parsed, unwrapped);
        if (IsInvalidProductName(parsed.ProductName))
        {
            parsed.ProductName = null;
        }

        return parsed;
    }

    private static void ApplyExpandedUrlName(ProductMetadataDto parsed, string resolvedUrl)
    {
        var slugName = ProductMetadataHtmlParser.TryExtractMarketplaceProductNameFromUrl(resolvedUrl);
        if (!IsInvalidProductName(slugName))
        {
            parsed.ProductName = slugName;
            return;
        }

        if (IsInvalidProductName(parsed.ProductName)
            || ProductMetadataHtmlParser.LooksLikeAntiBotTitle(parsed.ProductName)
            || string.IsNullOrWhiteSpace(parsed.ProductName))
        {
            var fallback = ProductMetadataHtmlParser.BuildSlugFallback(resolvedUrl);
            parsed.ProductName = IsInvalidProductName(fallback) ? null : fallback;
        }
    }

    private static void ApplyAntiBotBrowserHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1");
        request.Headers.TryAddWithoutValidation(
            "Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        request.Headers.TryAddWithoutValidation("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8");
    }

    private static string HttpUtilityUrlDecode(string value)
    {
        var current = value ?? string.Empty;
        for (var attempt = 0; attempt < 4; attempt++)
        {
            string decoded;
            try
            {
                decoded = Uri.UnescapeDataString(current.Replace("+", "%20"));
            }
            catch (UriFormatException)
            {
                decoded = current;
            }

            decoded = WebUtility.UrlDecode(decoded) ?? decoded;
            if (string.Equals(decoded, current, StringComparison.Ordinal))
            {
                break;
            }

            current = decoded;
        }

        return current;
    }
}

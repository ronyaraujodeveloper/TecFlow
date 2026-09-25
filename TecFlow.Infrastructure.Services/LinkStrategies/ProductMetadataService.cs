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

        try
        {
            if (string.IsNullOrWhiteSpace(resolvedUrl) || !Uri.TryCreate(resolvedUrl, UriKind.Absolute, out _))
            {
                return ProductMetadataHtmlParser.FromUrlFallback(workingUrl);
            }

            var client = _httpClientFactory.CreateClient(IntegrationHttpClientNames.ProductMetadata);
            using var request = new HttpRequestMessage(HttpMethod.Get, resolvedUrl);
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Scraping de metadados recusado. Status={Status} Url={Url}",
                    (int)response.StatusCode,
                    resolvedUrl);
                return ProductMetadataHtmlParser.FromUrlFallback(resolvedUrl);
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            if (html.Length > MaxHtmlChars)
            {
                html = html[..MaxHtmlChars];
            }

            var parsed = ProductMetadataHtmlParser.Parse(html, resolvedUrl);
            ApplyExpandedUrlName(parsed, resolvedUrl);
            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Falha ao extrair metadados do produto. Url={Url}",
                resolvedUrl);
            return ProductMetadataHtmlParser.FromUrlFallback(resolvedUrl);
        }
    }

    private static void ApplyExpandedUrlName(ProductMetadataDto parsed, string resolvedUrl)
    {
        var slugName = ProductMetadataHtmlParser.TryExtractMarketplaceProductNameFromUrl(resolvedUrl);
        if (!string.IsNullOrWhiteSpace(slugName))
        {
            parsed.ProductName = slugName;
            return;
        }

        if (ProductMetadataHtmlParser.LooksLikeAntiBotTitle(parsed.ProductName)
            || string.IsNullOrWhiteSpace(parsed.ProductName))
        {
            parsed.ProductName = ProductMetadataHtmlParser.BuildSlugFallback(resolvedUrl);
        }
    }
}

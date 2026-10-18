using System.Net;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.MercadoLivre;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class OfferValidationService : IOfferValidationService
{
    private readonly IProductMetadataService _metadata;
    private readonly IUrlResolverService _urlResolver;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OfferValidationService> _logger;

    public OfferValidationService(
        IProductMetadataService metadata,
        IUrlResolverService urlResolver,
        HttpClient httpClient,
        ILogger<OfferValidationService> logger)
    {
        _metadata = metadata;
        _urlResolver = urlResolver;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OfferValidationResultDto> ValidateAsync(
        string originalUrl,
        decimal? capturedPrice,
        CancellationToken cancellationToken = default)
    {
        var page = await ValidateProductPageStatusAsync(originalUrl, cancellationToken);
        return await ValidateFromPageAsync(page, originalUrl, capturedPrice, cancellationToken);
    }

    public async Task<OfferValidationResultDto> ValidateFromPageAsync(
        OfferPageStatusDto page,
        string originalUrl,
        decimal? capturedPrice,
        CancellationToken cancellationToken = default)
    {
        var platform = GroupOfferCaptureRules.DetectPlatform(page.FinalUrl ?? originalUrl);
        if (!page.IsAvailable)
        {
            return new OfferValidationResultDto
            {
                Status = page.Status,
                IsAvailable = false,
                Price = capturedPrice,
                Platform = platform,
                FinalUrl = page.FinalUrl
            };
        }

        try
        {
            ProductMetadataDto metadata;
            if (!string.IsNullOrWhiteSpace(page.Html))
            {
                metadata = ProductMetadataHtmlParser.Parse(page.Html, page.FinalUrl ?? originalUrl);
                if (GroupOfferCaptureRules.NeedsStructuredFallback(metadata.ProductName, metadata.ProductPrice))
                {
                    var extracted = await _metadata.ExtractAsync(page.FinalUrl ?? originalUrl, cancellationToken);
                    metadata = MergeMetadata(metadata, extracted);
                }
            }
            else
            {
                metadata = await _metadata.ExtractAsync(page.FinalUrl ?? originalUrl, cancellationToken);
            }

            if (ShopeeProductPageRules.IsShopeeUrl(page.FinalUrl ?? originalUrl)
                && ShopeeProductPageRules.TryExtractActivePrice(page.Html) is { } shopeePrice)
            {
                metadata.ProductPrice = shopeePrice;
            }

            if (OfferPriceTextRules.IsSignificantDivergence(capturedPrice, metadata.ProductPrice))
            {
                _logger.LogWarning(
                    "Divergência de preço detectada na página. Url={Url} Capturado={Captured} Pagina={PagePrice}",
                    originalUrl,
                    capturedPrice,
                    metadata.ProductPrice);
            }

            var price = metadata.ProductPrice ?? capturedPrice;
            var status = GroupOfferStatuses.Ativo;
            if (capturedPrice is > 0 && metadata.ProductPrice is > 0
                && Math.Abs(metadata.ProductPrice.Value - capturedPrice.Value) >= 0.01m)
            {
                status = GroupOfferStatuses.PrecoAlterado;
            }

            return new OfferValidationResultDto
            {
                Status = status,
                IsAvailable = true,
                Price = price,
                ImageUrl = metadata.ProductImageUrl,
                ProductName = metadata.ProductName,
                Platform = platform ?? GroupOfferCaptureRules.DetectPlatform(originalUrl),
                FinalUrl = page.FinalUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao extrair metadados na validação. Url={Url}", originalUrl);
            return new OfferValidationResultDto
            {
                Status = GroupOfferStatuses.Verificando,
                IsAvailable = true,
                Price = capturedPrice,
                Platform = platform,
                FinalUrl = page.FinalUrl
            };
        }
    }

    public async Task<OfferPageStatusDto> ValidateProductPageStatusAsync(
        string originalUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return new OfferPageStatusDto
            {
                IsAvailable = false,
                Status = GroupOfferStatuses.Esgotado,
                FinalUrl = originalUrl
            };
        }

        var workingUrl = BypassAffiliateShowcase(originalUrl, originalUrl.Trim());
        if (IsUnresolvableStorefront(workingUrl))
        {
            return GenericStorefront(originalUrl, workingUrl);
        }

        try
        {
            var resolved = await _urlResolver.ResolveCanonicalAsync(workingUrl, cancellationToken);
            if (!string.IsNullOrWhiteSpace(resolved.CanonicalUrl))
            {
                workingUrl = resolved.CanonicalUrl;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Follow redirects falhou na integridade da oferta. Url={Url}", originalUrl);
        }

        workingUrl = BypassAffiliateShowcase(originalUrl, workingUrl);
        if (IsUnresolvableStorefront(workingUrl))
        {
            return GenericStorefront(originalUrl, workingUrl);
        }

        try
        {
            var requestedUrl = workingUrl;
            var (pageResponse, finalUrl) = await GetProductPageAsync(originalUrl, workingUrl, cancellationToken);
            using var response = pageResponse;
            if (!string.IsNullOrWhiteSpace(finalUrl))
            {
                workingUrl = finalUrl;
            }

            if (OfferIntegrityRules.IsGenericStorefrontUrl(workingUrl))
            {
                return GenericStorefront(originalUrl, workingUrl);
            }

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone or HttpStatusCode.UnavailableForLegalReasons)
            {
                return Unavailable(workingUrl);
            }

            if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 600
                && response.StatusCode is not HttpStatusCode.Forbidden and not HttpStatusCode.Unauthorized)
            {
                return Unavailable(workingUrl);
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            if (OfferIntegrityRules.ContainsClosedListingPhrase(html))
            {
                _logger.LogInformation(
                    "Anúncio encerrado ou pausado detectado. Url={Url} FinalUrl={FinalUrl}",
                    originalUrl,
                    workingUrl);
                return new OfferPageStatusDto
                {
                    IsAvailable = false,
                    Status = GroupOfferStatuses.Inativo,
                    Html = html,
                    FinalUrl = workingUrl
                };
            }

            if (MercadoLivreAvailabilityRules.IsMercadoLivreUrl(workingUrl)
                && MercadoLivreAvailabilityRules.DetectUnavailableStatus(html, requestedUrl, workingUrl) is { } mercadoLivreStatus)
            {
                _logger.LogInformation(
                    "Anúncio Mercado Livre sem compra direta. Status={Status} Url={Url} FinalUrl={FinalUrl}",
                    mercadoLivreStatus,
                    originalUrl,
                    workingUrl);
                return new OfferPageStatusDto
                {
                    IsAvailable = false,
                    Status = mercadoLivreStatus,
                    Html = html,
                    FinalUrl = workingUrl
                };
            }

            if (ShopeeProductPageRules.IsShopeeUrl(workingUrl) && ShopeeProductPageRules.IsSoldOut(html))
            {
                _logger.LogInformation(
                    "Produto Shopee esgotado detectado. Url={Url} FinalUrl={FinalUrl}",
                    originalUrl,
                    workingUrl);
                return Unavailable(workingUrl, html);
            }

            if (GroupOfferCaptureRules.ContainsUnavailableProductPhrase(html))
            {
                return Unavailable(workingUrl, html);
            }

            return new OfferPageStatusDto
            {
                IsAvailable = true,
                Status = GroupOfferStatuses.Ativo,
                Html = html,
                FinalUrl = workingUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha HTTP ao validar anúncio. Url={Url}", originalUrl);
            return new OfferPageStatusDto
            {
                IsAvailable = true,
                Status = GroupOfferStatuses.Verificando,
                FinalUrl = workingUrl
            };
        }
    }

    /// <summary>
    /// Vitrine genérica que não vale seguir: vitrines Meli Social ainda passam pelo GET com redirects,
    /// que pode cair na página do produto; a rejeição delas acontece só na URL final.
    /// </summary>
    private static bool IsUnresolvableStorefront(string url) =>
        OfferIntegrityRules.IsGenericStorefrontUrl(url)
        && !MercadoLivreShowcaseRules.IsAffiliateShowcaseUrl(url);

    private string BypassAffiliateShowcase(string originalUrl, string url)
    {
        if (!MercadoLivreShowcaseRules.TryExtractFocusedProductUrl(url, out var productUrl))
        {
            return url;
        }

        _logger.LogInformation(
            "Vitrine de divulgador convertida no produto focado. Url={Url} Produto={ProductUrl}",
            originalUrl,
            productUrl);
        return productUrl;
    }

    private async Task<(HttpResponseMessage Response, string FinalUrl)> GetProductPageAsync(
        string originalUrl,
        string url,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
        var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;
        var productUrl = BypassAffiliateShowcase(originalUrl, finalUrl);
        if (string.Equals(productUrl, finalUrl, StringComparison.Ordinal))
        {
            return (response, finalUrl);
        }

        response.Dispose();
        var productResponse = await _httpClient.GetAsync(productUrl, cancellationToken);
        return (productResponse, productResponse.RequestMessage?.RequestUri?.ToString() ?? productUrl);
    }

    private OfferPageStatusDto GenericStorefront(string originalUrl, string finalUrl)
    {
        _logger.LogInformation(
            "Link de vitrine genérica rejeitado. Url={Url} FinalUrl={FinalUrl}",
            originalUrl,
            finalUrl);
        return new OfferPageStatusDto
        {
            IsAvailable = false,
            Status = GroupOfferStatuses.VitrineGenerica,
            FinalUrl = finalUrl
        };
    }

    private static OfferPageStatusDto Unavailable(string? finalUrl, string? html = null) =>
        new()
        {
            IsAvailable = false,
            Status = GroupOfferStatuses.Esgotado,
            Html = html,
            FinalUrl = finalUrl
        };

    private static ProductMetadataDto MergeMetadata(ProductMetadataDto primary, ProductMetadataDto fallback) =>
        new()
        {
            ProductName = FirstNonEmpty(primary.ProductName, fallback.ProductName),
            ProductPrice = primary.ProductPrice is > 0 ? primary.ProductPrice : fallback.ProductPrice,
            ProductImageUrl = FirstNonEmpty(primary.ProductImageUrl, fallback.ProductImageUrl)
        };

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}

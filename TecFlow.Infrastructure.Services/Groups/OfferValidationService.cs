using System.Net;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class OfferValidationService : IOfferValidationService
{
    private readonly IProductMetadataService _metadata;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OfferValidationService> _logger;

    public OfferValidationService(
        IProductMetadataService metadata,
        HttpClient httpClient,
        ILogger<OfferValidationService> logger)
    {
        _metadata = metadata;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(string Status, decimal? Price, string? ImageUrl, string? ProductName, MarketplaceType? Platform)>
        ValidateAsync(string originalUrl, decimal? capturedPrice, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return (GroupOfferStatuses.Esgotado, capturedPrice, null, null, null);
        }

        try
        {
            using var response = await _httpClient.GetAsync(originalUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone or HttpStatusCode.UnavailableForLegalReasons)
            {
                return (GroupOfferStatuses.Esgotado, capturedPrice, null, null, GroupOfferCaptureRules.DetectPlatform(originalUrl));
            }

            if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 600
                && response.StatusCode is not HttpStatusCode.Forbidden and not HttpStatusCode.Unauthorized)
            {
                return (GroupOfferStatuses.Esgotado, capturedPrice, null, null, GroupOfferCaptureRules.DetectPlatform(originalUrl));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha HTTP ao validar anúncio. Url={Url}", originalUrl);
        }

        try
        {
            var metadata = await _metadata.ExtractAsync(originalUrl, cancellationToken);
            var platform = GroupOfferCaptureRules.DetectPlatform(originalUrl);
            var price = metadata.ProductPrice ?? capturedPrice;
            var status = GroupOfferStatuses.Ativo;
            if (capturedPrice is > 0 && metadata.ProductPrice is > 0
                && Math.Abs(metadata.ProductPrice.Value - capturedPrice.Value) >= 0.01m)
            {
                status = GroupOfferStatuses.PrecoAlterado;
            }

            return (status, price, metadata.ProductImageUrl, metadata.ProductName, platform);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao extrair metadados na validação. Url={Url}", originalUrl);
            return (GroupOfferStatuses.Verificando, capturedPrice, null, null, GroupOfferCaptureRules.DetectPlatform(originalUrl));
        }
    }
}

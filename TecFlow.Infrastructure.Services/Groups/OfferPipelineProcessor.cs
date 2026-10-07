using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class OfferPipelineProcessor : IOfferPipelineProcessor
{
    private readonly IStructuredOfferParserService _parser;
    private readonly IGroupCapturedMessagesService _capturedMessages;
    private readonly IOfferValidationService _validation;
    private readonly IOfferProductMediaStore _mediaStore;
    private readonly ILogger<OfferPipelineProcessor> _logger;

    public OfferPipelineProcessor(
        IStructuredOfferParserService parser,
        IGroupCapturedMessagesService capturedMessages,
        IOfferValidationService validation,
        IOfferProductMediaStore mediaStore,
        ILogger<OfferPipelineProcessor> logger)
    {
        _parser = parser;
        _capturedMessages = capturedMessages;
        _validation = validation;
        _mediaStore = mediaStore;
        _logger = logger;
    }

    public async Task<int> ProcessAsync(
        GroupOfferCaptureRequest request,
        bool requireLocalPhoto,
        CancellationToken cancellationToken = default)
    {
        var extracted = _parser.Parse(request.RawText);
        var urls = StructuredOfferParserService.FilterPersistableUrls(
            WhatsAppBotRules.ExtractUrls(request.RawText),
            extracted);
        if (urls.Count == 0)
        {
            return 0;
        }

        var platforms = await _capturedMessages.ListActivePlatformsAsync(request.UserId, cancellationToken);
        if (platforms.Count == 0)
        {
            return 0;
        }

        string? localPhotoUrl = ProductImageStorageRules.IsLocalProductImage(request.ProductImageUrl)
            ? ProductImageStorageRules.ToWebRelativePath(request.ProductImageUrl)
            : null;
        if (request.PhotoBytes is { Length: > 0 })
        {
            localPhotoUrl = await _mediaStore.SaveProductPhotoAsync(
                request.UserId,
                request.ExternalMessageId,
                request.PhotoBytes,
                cancellationToken) ?? localPhotoUrl;
        }

        if (requireLocalPhoto && !ProductImageStorageRules.IsLocalProductImage(localPhotoUrl))
        {
            _logger.LogDebug(
                "Pacote atômico descartado: foto essencial ausente. UserId={UserId} MessageId={MessageId}",
                request.UserId,
                request.ExternalMessageId);
            return 0;
        }

        var groupKey = GroupOfferCaptureRules.BuildGroupKey(request.Channel, request.GroupId);
        var groupName = string.IsNullOrWhiteSpace(request.GroupName) ? request.GroupId : request.GroupName.Trim();
        var saved = 0;

        foreach (var url in urls)
        {
            try
            {
                var platform = GroupOfferCaptureRules.DetectPlatform(url)
                    ?? GroupOfferCaptureRules.DetectPlatform(extracted.PrimaryProductUrl);
                if (platform is null || !platforms.Contains(platform.Value))
                {
                    continue;
                }

                var name = FirstNonEmpty(extracted.ProductTitle, GroupOfferCaptureRules.ExtractName(request.RawText, url));
                var price = extracted.Price ?? GroupOfferCaptureRules.ExtractPrice(request.RawText, url);
                OfferValidationResultDto? validation = null;
                try
                {
                    validation = await _validation.ValidateAsync(url, price, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Validação do pacote atômico falhou. Url={Url}", url);
                    continue;
                }

                if (validation is null || !validation.IsAvailable)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(validation.ProductName))
                {
                    name = validation.ProductName;
                }

                if (price is not > 0 && validation.Price is > 0)
                {
                    price = validation.Price;
                }

                if (validation.Platform is { } validatedPlatform)
                {
                    platform = validatedPlatform;
                    if (!platforms.Contains(platform.Value))
                    {
                        continue;
                    }
                }

                var image = ProductImageStorageRules.IsLocalProductImage(localPhotoUrl)
                    ? ProductImageStorageRules.ToWebRelativePath(localPhotoUrl)
                    : null;
                if (!requireLocalPhoto
                    && string.IsNullOrWhiteSpace(image)
                    && ProductImageStorageRules.IsLocalProductImage(validation.ImageUrl))
                {
                    image = ProductImageStorageRules.ToWebRelativePath(validation.ImageUrl);
                }

                if (!GroupOfferCaptureRules.IsCompleteAtomicPackage(
                    request.Channel,
                    name,
                    price,
                    image,
                    platform))
                {
                    continue;
                }

                var entity = new GroupCapturedMessage
                {
                    UserId = request.UserId,
                    Channel = request.Channel,
                    GroupKey = groupKey,
                    GroupName = groupName.Length <= 256 ? groupName : groupName[..256],
                    ExternalMessageId = request.ExternalMessageId,
                    RawText = request.RawText,
                    MediaUrl = Truncate(image, 500),
                    ProductImageUrl = Truncate(image, 500),
                    OriginalUrl = Truncate(url, 1000) ?? url,
                    PrimaryProductUrl = Truncate(extracted.PrimaryProductUrl, 1000) ?? Truncate(url, 1000),
                    ProductName = name,
                    CouponCode = Truncate(extracted.CouponCode, 64),
                    ExtractedPrice = price,
                    ValidatedPrice = validation.Price,
                    PlatformType = platform,
                    PlatformName = FirstNonEmpty(extracted.Platform, platform?.GetDisplayName()),
                    OfferStatus = validation.Status,
                    ReceivedAt = request.ReceivedAt == default ? DateTime.UtcNow : request.ReceivedAt,
                    LastValidatedAt = DateTime.UtcNow,
                    HasDirectProductUrl = GroupOfferCaptureRules.HasDirectProductUrl(url),
                    IsAvailable = validation.IsAvailable
                };
                _capturedMessages.ApplyStructuredParse(entity, request.RawText);
                saved += await _capturedMessages.SaveValidatedOfferAsync(entity, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha no pacote atômico. UserId={UserId} Url={Url}", request.UserId, url);
            }
        }

        return saved;
    }

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

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}

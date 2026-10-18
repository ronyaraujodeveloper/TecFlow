using Microsoft.Extensions.Logging;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Groups;
using Ofertio.Business.Service.LinkStrategies;
using Ofertio.Business.Service.WhatsApp;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;

namespace Ofertio.Infrastructure.Services.Groups;

public sealed class OfferPipelineProcessor : IOfferPipelineProcessor
{
    private readonly IStructuredOfferParserService _parser;
    private readonly IGroupCapturedMessagesService _capturedMessages;
    private readonly IOfferValidationService _validation;
    private readonly IOfferProductMediaStore _mediaStore;
    private readonly IPriceHistoryTracker _priceHistory;
    private readonly ILogger<OfferPipelineProcessor> _logger;

    public OfferPipelineProcessor(
        IStructuredOfferParserService parser,
        IGroupCapturedMessagesService capturedMessages,
        IOfferValidationService validation,
        IOfferProductMediaStore mediaStore,
        IPriceHistoryTracker priceHistory,
        ILogger<OfferPipelineProcessor> logger)
    {
        _parser = parser;
        _capturedMessages = capturedMessages;
        _validation = validation;
        _mediaStore = mediaStore;
        _priceHistory = priceHistory;
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
        var offerType = CouponMessageRules.Classify(
            request.RawText,
            urls.Any(GroupOfferCaptureRules.HasDirectProductUrl));
        if (offerType == OfferType.Coupon)
        {
            var couponPhoto = await ResolveLocalPhotoAsync(request, cancellationToken);
            return await SaveCouponAsync(request, extracted, urls, couponPhoto, cancellationToken);
        }

        if (urls.Count == 0)
        {
            return 0;
        }

        var platforms = await _capturedMessages.ListActivePlatformsAsync(request.UserId, cancellationToken);
        if (platforms.Count == 0)
        {
            return 0;
        }

        var localPhotoUrl = await ResolveLocalPhotoAsync(request, cancellationToken);

        if (requireLocalPhoto && !ProductImageStorageRules.IsLocalProductImage(localPhotoUrl))
        {
            _logger.LogWarning(
                "Pacote atômico descartado: foto essencial ausente. UserId={UserId} MessageId={MessageId}",
                request.UserId,
                request.ExternalMessageId);
            return 0;
        }

        var groupKey = GroupOfferCaptureRules.BuildGroupKey(request.Channel, request.GroupId);
        var groupName = ResolveGroupName(request);
        var saved = 0;

        foreach (var capturedUrl in urls)
        {
            var url = MercadoLivreShowcaseRules.TryExtractFocusedProductUrl(capturedUrl, out var focusedProductUrl)
                ? focusedProductUrl
                : capturedUrl;
            try
            {
                var platform = GroupOfferCaptureRules.DetectPlatform(url)
                    ?? GroupOfferCaptureRules.DetectPlatform(extracted.PrimaryProductUrl);
                if (platform is null || !platforms.Contains(platform.Value))
                {
                    continue;
                }

                if (OfferIntegrityRules.IsGenericStorefrontUrl(url)
                    && !MercadoLivreShowcaseRules.IsAffiliateShowcaseUrl(url))
                {
                    _logger.LogInformation(
                        "Ingestão rejeitada: VitrineGenerica. UserId={UserId} MessageId={MessageId} Url={Url}",
                        request.UserId,
                        request.ExternalMessageId,
                        url);
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
                    if (validation is not null)
                    {
                        _logger.LogInformation(
                            "Ingestão rejeitada: {Status}. UserId={UserId} MessageId={MessageId} Url={Url} FinalUrl={FinalUrl}",
                            validation.Status,
                            request.UserId,
                            request.ExternalMessageId,
                            url,
                            validation.FinalUrl);
                    }

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
                else if (OfferPriceTextRules.IsSignificantDivergence(price, validation.Price))
                {
                    _logger.LogWarning(
                        "Divergência de preço corrigida na ingestão. UserId={UserId} MessageId={MessageId} Url={Url} Mensagem={Captured} Pagina={PagePrice}",
                        request.UserId,
                        request.ExternalMessageId,
                        url,
                        price,
                        validation.Price);
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

                var status = OfferIntegrityRules.ApplyPriceSanity(validation.Status, name, validation.Price ?? price);
                if (status == GroupOfferStatuses.PrecoSuspeito)
                {
                    _logger.LogWarning(
                        "Oferta sinalizada como PrecoSuspeito. UserId={UserId} MessageId={MessageId} Produto={Product} Preco={Price}",
                        request.UserId,
                        request.ExternalMessageId,
                        name,
                        validation.Price ?? price);
                }

                var entity = new GroupCapturedMessage
                {
                    UserId = request.UserId,
                    Channel = request.Channel,
                    GroupKey = groupKey,
                    GroupName = groupName,
                    ExternalMessageId = request.ExternalMessageId,
                    RawText = request.RawText,
                    MediaUrl = Truncate(image, 500),
                    ProductImageUrl = Truncate(image, 500),
                    OriginalUrl = Truncate(capturedUrl, 1000) ?? capturedUrl,
                    PrimaryProductUrl = Truncate(ResolvePrimaryProductUrl(url, validation.FinalUrl, extracted.PrimaryProductUrl), 1000),
                    ProductName = name,
                    CouponCode = Truncate(extracted.CouponCode, 64),
                    ExtractedPrice = price,
                    ValidatedPrice = validation.Price,
                    PlatformType = platform,
                    PlatformName = FirstNonEmpty(extracted.Platform, platform?.GetDisplayName()),
                    OfferStatus = status,
                    ReceivedAt = CapturedMessageTimeRules.ResolveReceivedAtUtc(request.ReceivedAt, DateTime.UtcNow),
                    LastValidatedAt = DateTime.UtcNow,
                    HasDirectProductUrl = GroupOfferCaptureRules.HasDirectProductUrl(url),
                    IsAvailable = validation.IsAvailable,
                    Type = OfferType.Product
                };
                _capturedMessages.ApplyStructuredParse(entity, request.RawText);
                saved += await _capturedMessages.SaveValidatedOfferAsync(entity, cancellationToken);
                if (saved > 0)
                {
                    await _priceHistory.RecordAsync(
                        entity.PrimaryProductUrl ?? entity.OriginalUrl,
                        entity.PlatformType,
                        entity.ValidatedPrice ?? entity.ExtractedPrice,
                        entity.ReceivedAt,
                        cancellationToken);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha no pacote atômico. UserId={UserId} Url={Url}", request.UserId, url);
            }
        }

        return saved;
    }

    private async Task<int> SaveCouponAsync(
        GroupOfferCaptureRequest request,
        OfferDataExtraction extracted,
        IReadOnlyList<string> urls,
        string? localPhotoUrl,
        CancellationToken cancellationToken)
    {
        var url = urls.FirstOrDefault(GroupOfferCaptureRules.HasDirectProductUrl)
            ?? urls.FirstOrDefault(item => !StructuredOfferParserService.IsShowcaseUrl(item))
            ?? urls.FirstOrDefault();
        var code = CouponMessageRules.ExtractCouponCode(request.RawText)
            ?? (string.IsNullOrWhiteSpace(extracted.CouponCode) ? null : extracted.CouponCode.Trim());
        if (string.IsNullOrWhiteSpace(url) && code is null)
        {
            return 0;
        }

        var platform = GroupOfferCaptureRules.DetectPlatform(url)
            ?? GroupOfferCaptureRules.DetectPlatform(extracted.PrimaryProductUrl)
            ?? CouponMessageRules.DetectPlatformFromText(request.RawText);
        var image = ProductImageStorageRules.IsLocalProductImage(localPhotoUrl)
            ? ProductImageStorageRules.ToWebRelativePath(localPhotoUrl)
            : null;
        var entity = new GroupCapturedMessage
        {
            UserId = request.UserId,
            Channel = request.Channel,
            GroupKey = GroupOfferCaptureRules.BuildGroupKey(request.Channel, request.GroupId),
            GroupName = ResolveGroupName(request),
            ExternalMessageId = request.ExternalMessageId,
            RawText = request.RawText,
            MediaUrl = Truncate(image, 500),
            ProductImageUrl = Truncate(image, 500),
            OriginalUrl = Truncate(url, 1000) ?? string.Empty,
            PrimaryProductUrl = Truncate(url, 1000),
            ProductName = Truncate(CouponMessageRules.ExtractHeadline(request.RawText), 255),
            CouponCode = Truncate(code, 64),
            PlatformType = platform,
            PlatformName = platform?.GetDisplayName(),
            OfferStatus = GroupOfferStatuses.Ativo,
            ReceivedAt = CapturedMessageTimeRules.ResolveReceivedAtUtc(request.ReceivedAt, DateTime.UtcNow),
            HasDirectProductUrl = GroupOfferCaptureRules.HasDirectProductUrl(url),
            IsAvailable = true,
            Type = OfferType.Coupon
        };

        var saved = await _capturedMessages.SaveValidatedOfferAsync(entity, cancellationToken);
        if (saved > 0)
        {
            _logger.LogInformation(
                "Cupom capturado. UserId={UserId} MessageId={MessageId} Plataforma={Platform} Codigo={Code}",
                request.UserId,
                request.ExternalMessageId,
                platform,
                entity.CouponCode);
        }

        return saved;
    }

    private async Task<string?> ResolveLocalPhotoAsync(
        GroupOfferCaptureRequest request,
        CancellationToken cancellationToken)
    {
        string? localPhotoUrl = ProductImageStorageRules.IsLocalProductImage(request.ProductImageUrl)
            && _mediaStore.ExistsOnDisk(request.ProductImageUrl)
                ? ProductImageStorageRules.ToWebRelativePath(request.ProductImageUrl)
                : null;
        if (localPhotoUrl is null && ProductImageStorageRules.IsLocalProductImage(request.ProductImageUrl))
        {
            _logger.LogWarning(
                "Foto local da oferta ausente no disco; URL ignorada. UserId={UserId} MessageId={MessageId} Url={Url}",
                request.UserId,
                request.ExternalMessageId,
                request.ProductImageUrl);
        }
        if (request.PhotoBytes is { Length: > 0 })
        {
            localPhotoUrl = await _mediaStore.SaveProductPhotoAsync(
                request.UserId,
                request.ExternalMessageId,
                request.PhotoBytes,
                cancellationToken) ?? localPhotoUrl;
        }

        return localPhotoUrl;
    }

    private static string? ResolvePrimaryProductUrl(string url, string? finalUrl, string? extractedPrimaryUrl)
    {
        if (MercadoLivreShowcaseRules.TryResolveDirectProductUrl(finalUrl, out var resolved))
        {
            return resolved;
        }

        return MercadoLivreShowcaseRules.TryExtractFocusedProductUrl(extractedPrimaryUrl, out var focused)
            ? focused
            : FirstNonEmpty(extractedPrimaryUrl, url);
    }

    private static string ResolveGroupName(GroupOfferCaptureRequest request)
    {
        var groupName = string.IsNullOrWhiteSpace(request.GroupName) ? request.GroupId : request.GroupName.Trim();
        return groupName.Length <= 256 ? groupName : groupName[..256];
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

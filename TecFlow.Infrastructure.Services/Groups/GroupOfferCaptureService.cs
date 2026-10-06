using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class GroupOfferCaptureService : IGroupOfferCaptureService
{
    private readonly AppDbContext _context;
    private readonly IOfferValidationService _validation;
    private readonly IOfferProductMediaStore _mediaStore;
    private readonly IStructuredOfferParserService _parser;
    private readonly IGroupCapturedMessagesService _capturedMessages;
    private readonly ILogger<GroupOfferCaptureService> _logger;

    public GroupOfferCaptureService(
        AppDbContext context,
        IOfferValidationService validation,
        IOfferProductMediaStore mediaStore,
        IStructuredOfferParserService parser,
        IGroupCapturedMessagesService capturedMessages,
        ILogger<GroupOfferCaptureService> logger)
    {
        _context = context;
        _validation = validation;
        _mediaStore = mediaStore;
        _parser = parser;
        _capturedMessages = capturedMessages;
        _logger = logger;
    }

    public async Task CaptureAsync(GroupOfferCaptureRequest request, CancellationToken cancellationToken = default)
    {
        var extracted = _parser.Parse(request.RawText);
        var urls = StructuredOfferParserService.FilterPersistableUrls(
            WhatsAppBotRules.ExtractUrls(request.RawText),
            extracted);
        if (urls.Count == 0)
        {
            return;
        }

        var groupKey = GroupOfferCaptureRules.BuildGroupKey(request.Channel, request.GroupId);
        var groupName = string.IsNullOrWhiteSpace(request.GroupName) ? request.GroupId : request.GroupName.Trim();
        string? localPhotoUrl = null;
        if (request.PhotoBytes is { Length: > 0 })
        {
            localPhotoUrl = await _mediaStore.SaveProductPhotoAsync(
                request.UserId,
                request.ExternalMessageId,
                request.PhotoBytes,
                cancellationToken);
        }

        var added = new List<GroupCapturedMessage>();
        foreach (var url in urls)
        {
            try
            {
                var exists = await _context.GroupCapturedMessages.AnyAsync(
                    item => item.UserId == request.UserId
                        && item.Channel == request.Channel
                        && item.OriginalUrl == url
                        && item.ExternalMessageId == request.ExternalMessageId,
                    cancellationToken);
                if (exists)
                {
                    continue;
                }

                var platform = GroupOfferCaptureRules.DetectPlatform(url)
                    ?? GroupOfferCaptureRules.DetectPlatform(extracted.PrimaryProductUrl);
                var name = FirstNonEmpty(extracted.ProductTitle, GroupOfferCaptureRules.ExtractName(request.RawText, url));
                var price = extracted.Price ?? GroupOfferCaptureRules.ExtractPrice(request.RawText, url);
                var image = ProductMetadataHtmlParser.NormalizePersistedProductImageUrl(localPhotoUrl)
                    ?? ProductMetadataHtmlParser.NormalizePersistedProductImageUrl(request.MediaUrl);
                var entity = new GroupCapturedMessage
                {
                    UserId = request.UserId,
                    Channel = request.Channel,
                    GroupKey = groupKey,
                    GroupName = groupName.Length <= 256 ? groupName : groupName[..256],
                    ExternalMessageId = request.ExternalMessageId,
                    RawText = request.RawText,
                    MediaUrl = Truncate(ProductImageStorageRules.ToWebRelativePath(localPhotoUrl ?? request.MediaUrl), 500),
                    ProductImageUrl = Truncate(ProductImageStorageRules.ToWebRelativePath(image), 500),
                    OriginalUrl = Truncate(url, 1000) ?? url,
                    PrimaryProductUrl = Truncate(extracted.PrimaryProductUrl, 1000) ?? Truncate(url, 1000),
                    ProductName = name,
                    CouponCode = Truncate(extracted.CouponCode, 64),
                    ExtractedPrice = price,
                    PlatformType = platform,
                    PlatformName = FirstNonEmpty(extracted.Platform, platform?.GetDisplayName()),
                    OfferStatus = GroupOfferStatuses.Verificando,
                    ReceivedAt = request.ReceivedAt == default ? DateTime.UtcNow : request.ReceivedAt,
                    HasDirectProductUrl = GroupOfferCaptureRules.HasDirectProductUrl(url),
                    IsAvailable = true
                };
                _capturedMessages.ApplyStructuredParse(entity, request.RawText);
                _context.GroupCapturedMessages.Add(entity);
                added.Add(entity);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao capturar oferta de grupo. UserId={UserId} Url={Url}", request.UserId, url);
            }
        }

        if (added.Count == 0)
        {
            return;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var entity in added)
        {
            await EnrichAsync(entity, string.IsNullOrWhiteSpace(localPhotoUrl), cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnrichAsync(GroupCapturedMessage entity, bool allowOgImage, CancellationToken cancellationToken)
    {
        try
        {
            var validation = await _validation.ValidateAsync(entity.OriginalUrl, entity.ExtractedPrice, cancellationToken);
            entity.OfferStatus = validation.Status;
            entity.IsAvailable = validation.IsAvailable;
            entity.ValidatedPrice = validation.Price;
            entity.LastValidatedAt = DateTime.UtcNow;
            if (validation.Price is > 0 && entity.ExtractedPrice is not > 0)
            {
                entity.ExtractedPrice = validation.Price;
            }

            if (GroupOfferCaptureRules.NeedsStructuredFallback(entity.ProductName, entity.ExtractedPrice)
                && !string.IsNullOrWhiteSpace(validation.ProductName))
            {
                entity.ProductName = validation.ProductName;
            }

            if (allowOgImage && string.IsNullOrWhiteSpace(entity.ProductImageUrl)
                && !string.IsNullOrWhiteSpace(validation.ImageUrl))
            {
                entity.ProductImageUrl = ProductImageStorageRules.ToWebRelativePath(
                    ProductMetadataHtmlParser.NormalizePersistedProductImageUrl(validation.ImageUrl));
            }

            if (validation.Platform is { } platform)
            {
                entity.PlatformType = platform;
                if (string.IsNullOrWhiteSpace(entity.PlatformName))
                {
                    entity.PlatformName = platform.GetDisplayName();
                }

                entity.HasDirectProductUrl = GroupOfferCaptureRules.HasDirectProductUrl(entity.OriginalUrl);
            }

            entity.Touch();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enriquecer oferta capturada. Id={Id} Url={Url}", entity.Id, entity.OriginalUrl);
        }
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

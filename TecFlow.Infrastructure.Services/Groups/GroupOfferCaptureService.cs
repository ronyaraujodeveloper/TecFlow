using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class GroupOfferCaptureService : IGroupOfferCaptureService
{
    private readonly AppDbContext _context;
    private readonly IOfferValidationService _validation;
    private readonly IOfferProductMediaStore _mediaStore;
    private readonly ILogger<GroupOfferCaptureService> _logger;

    public GroupOfferCaptureService(
        AppDbContext context,
        IOfferValidationService validation,
        IOfferProductMediaStore mediaStore,
        ILogger<GroupOfferCaptureService> logger)
    {
        _context = context;
        _validation = validation;
        _mediaStore = mediaStore;
        _logger = logger;
    }

    public async Task CaptureAsync(GroupOfferCaptureRequest request, CancellationToken cancellationToken = default)
    {
        var urls = WhatsAppBotRules.ExtractUrls(request.RawText);
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

                var platform = GroupOfferCaptureRules.DetectPlatform(url);
                var name = GroupOfferCaptureRules.ExtractName(request.RawText, url);
                var price = GroupOfferCaptureRules.ExtractPrice(request.RawText, url);
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
                    MediaUrl = Truncate(localPhotoUrl ?? request.MediaUrl, 500),
                    ProductImageUrl = Truncate(image, 500),
                    OriginalUrl = url.Length <= 1000 ? url : url[..1000],
                    ProductName = name,
                    ExtractedPrice = price,
                    PlatformType = platform,
                    PlatformName = platform?.ToString(),
                    OfferStatus = GroupOfferStatuses.Verificando,
                    ReceivedAt = request.ReceivedAt == default ? DateTime.UtcNow : request.ReceivedAt,
                    HasDirectProductUrl = GroupOfferCaptureRules.HasDirectProductUrl(url),
                    IsAvailable = true
                };
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
                entity.ProductImageUrl = ProductMetadataHtmlParser.NormalizePersistedProductImageUrl(validation.ImageUrl);
            }

            if (validation.Platform is { } platform)
            {
                entity.PlatformType = platform;
                entity.PlatformName = platform.ToString();
                entity.HasDirectProductUrl = GroupOfferCaptureRules.HasDirectProductUrl(entity.OriginalUrl);
            }

            entity.Touch();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enriquecer oferta capturada. Id={Id} Url={Url}", entity.Id, entity.OriginalUrl);
        }
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

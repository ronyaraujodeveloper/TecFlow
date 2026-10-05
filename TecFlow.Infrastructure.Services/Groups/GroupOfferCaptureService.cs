using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class GroupOfferCaptureService : IGroupOfferCaptureService
{
    private readonly AppDbContext _context;
    private readonly ILogger<GroupOfferCaptureService> _logger;

    public GroupOfferCaptureService(AppDbContext context, ILogger<GroupOfferCaptureService> logger)
    {
        _context = context;
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
                _context.GroupCapturedMessages.Add(new GroupCapturedMessage
                {
                    UserId = request.UserId,
                    Channel = request.Channel,
                    GroupKey = groupKey,
                    GroupName = groupName.Length <= 256 ? groupName : groupName[..256],
                    ExternalMessageId = request.ExternalMessageId,
                    RawText = request.RawText,
                    MediaUrl = request.MediaUrl,
                    ProductImageUrl = request.MediaUrl,
                    OriginalUrl = url.Length <= 1000 ? url : url[..1000],
                    ProductName = GroupOfferCaptureRules.ExtractName(request.RawText, url),
                    ExtractedPrice = GroupOfferCaptureRules.ExtractPrice(request.RawText, url),
                    PlatformType = platform,
                    PlatformName = platform?.ToString(),
                    OfferStatus = GroupOfferStatuses.Verificando,
                    ReceivedAt = request.ReceivedAt == default ? DateTime.UtcNow : request.ReceivedAt,
                    HasDirectProductUrl = GroupOfferCaptureRules.HasDirectProductUrl(url)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao capturar oferta de grupo. UserId={UserId} Url={Url}", request.UserId, url);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

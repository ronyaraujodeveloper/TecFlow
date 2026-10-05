using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class OfferHealthService : IOfferHealthService
{
    private readonly AppDbContext _context;
    private readonly IOfferValidationService _validation;
    private readonly ILogger<OfferHealthService> _logger;

    public OfferHealthService(
        AppDbContext context,
        IOfferValidationService validation,
        ILogger<OfferHealthService> logger)
    {
        _context = context;
        _validation = validation;
        _logger = logger;
    }

    public async Task<OfferHealthAlertResponseDto> ListAsync(int userId, CancellationToken cancellationToken = default)
    {
        var items = await _context.OfferHealthAlerts
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(80)
            .ToListAsync(cancellationToken);

        return new OfferHealthAlertResponseDto
        {
            Status = true,
            Alerts = items.Select(item => new OfferHealthAlertDto
            {
                Id = item.Id,
                Channel = item.Channel,
                CampaignId = item.CampaignId,
                ProductUrl = item.ProductUrl,
                CouponCode = item.CouponCode,
                AlertType = item.AlertType,
                Message = item.Message,
                Price = item.Price,
                ComparedPrice = item.ComparedPrice,
                IsRead = item.IsRead,
                CreatedAt = item.CreatedAt
            }).ToList()
        };
    }

    public async Task<int> InspectRecentCampaignsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddHours(-48);
        var whats = await _context.WhatsAppBroadcastCampaigns
            .AsNoTracking()
            .Where(item => item.UserId == userId
                && item.Status == WhatsAppBroadcastStatuses.Completed
                && item.UpdatedAt >= since)
            .ToListAsync(cancellationToken);
        var telegram = await _context.TelegramBroadcastCampaigns
            .AsNoTracking()
            .Where(item => item.UserId == userId
                && item.Status == TelegramBroadcastStatuses.Completed
                && item.UpdatedAt >= since)
            .ToListAsync(cancellationToken);

        var created = 0;
        foreach (var campaign in whats)
        {
            created += await InspectMessageAsync(userId, "WhatsApp", campaign.Id, campaign.MessageText, cancellationToken);
        }

        foreach (var campaign in telegram)
        {
            created += await InspectMessageAsync(userId, "Telegram", campaign.Id, campaign.MessageText, cancellationToken);
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return created;
    }

    private async Task<int> InspectMessageAsync(
        int userId,
        string channel,
        int campaignId,
        string? message,
        CancellationToken cancellationToken)
    {
        var added = 0;
        foreach (var url in OfferAttributionRules.ExtractUrls(message))
        {
            try
            {
                var coupon = AffiliateMiningRules.ExtractCoupon(message);
                var result = await _validation.ValidateAsync(url, null, cancellationToken);
                var exists = await _context.OfferHealthAlerts.AnyAsync(
                    item => item.UserId == userId
                        && item.CampaignId == campaignId
                        && item.ProductUrl == url
                        && item.CreatedAt >= DateTime.UtcNow.AddHours(-12),
                    cancellationToken);
                if (exists)
                {
                    continue;
                }

                if (!result.IsAvailable || result.Status == GroupOfferStatuses.Esgotado)
                {
                    _context.OfferHealthAlerts.Add(Alert(userId, channel, campaignId, url, coupon, OfferHealthAlertTypes.Esgotado, "Produto esgotado ou página quebrada após o disparo.", result.Price, null));
                    added++;
                }
                else if (result.Status == GroupOfferStatuses.PrecoAlterado)
                {
                    _context.OfferHealthAlerts.Add(Alert(userId, channel, campaignId, url, coupon, OfferHealthAlertTypes.PrecoAlterado, $"Preço alterado após o disparo: {result.Price:0.00}.", result.Price, null));
                    added++;
                }

                if (!string.IsNullOrWhiteSpace(coupon)
                    && !string.IsNullOrWhiteSpace(result.ProductName)
                    && result.ProductName.Contains(coupon, StringComparison.OrdinalIgnoreCase) is false)
                {
                    var page = await _validation.ValidateProductPageStatusAsync(url, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(page.Html)
                        && !page.Html.Contains(coupon, StringComparison.OrdinalIgnoreCase))
                    {
                        _context.OfferHealthAlerts.Add(Alert(userId, channel, campaignId, url, coupon, OfferHealthAlertTypes.CupomInvalido, $"Cupom {coupon} não encontrado na página do produto.", result.Price, null));
                        added++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Inspeção de saúde falhou. Url={Url}", url);
            }
        }

        return added;
    }

    private static OfferHealthAlert Alert(
        int userId,
        string channel,
        int campaignId,
        string url,
        string? coupon,
        string type,
        string message,
        decimal? price,
        decimal? compared) =>
        new()
        {
            UserId = userId,
            Channel = channel,
            CampaignId = campaignId,
            ProductUrl = url.Length <= 1000 ? url : url[..1000],
            CouponCode = coupon,
            AlertType = type,
            Message = message,
            Price = price,
            ComparedPrice = compared
        };
}

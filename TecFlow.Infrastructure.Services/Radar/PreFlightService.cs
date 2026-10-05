using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class PreFlightService : IPreFlightService
{
    private readonly AppDbContext _context;
    private readonly IOfferValidationService _validation;
    private readonly IProductArbitrageService _arbitrage;
    private readonly ILogger<PreFlightService> _logger;

    public PreFlightService(
        AppDbContext context,
        IOfferValidationService validation,
        IProductArbitrageService arbitrage,
        ILogger<PreFlightService> logger)
    {
        _context = context;
        _validation = validation;
        _arbitrage = arbitrage;
        _logger = logger;
    }

    public async Task<PreFlightNotificationResponseDto> ListAsync(int userId, CancellationToken cancellationToken = default)
    {
        var items = await _context.PreFlightNotifications
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.IsResolved)
            .ThenByDescending(item => item.CreatedAt)
            .Take(80)
            .ToListAsync(cancellationToken);

        return new PreFlightNotificationResponseDto
        {
            Status = true,
            Descricao = items.Exists(item => !item.IsResolved)
                ? "Há disparos pausados pelo pre-flight. Revise ou substitua o link."
                : string.Empty,
            Items = items.Select(Map).ToList()
        };
    }

    public async Task<int> InspectUpcomingAsync(CancellationToken cancellationToken = default)
    {
        var horizon = DateTime.UtcNow.Add(PreFlightRules.Lookahead);
        var whats = await _context.WhatsAppBroadcastCampaigns
            .Where(item => item.Status == WhatsAppBroadcastStatuses.Pending && item.ScheduledAt <= horizon)
            .ToListAsync(cancellationToken);
        var telegram = await _context.TelegramBroadcastCampaigns
            .Where(item => item.Status == TelegramBroadcastStatuses.Pending && item.ScheduledAt <= horizon)
            .ToListAsync(cancellationToken);

        var paused = 0;
        foreach (var campaign in whats)
        {
            if (await InspectCampaignAsync("WhatsApp", campaign.UserId, campaign.Id, campaign.Title, campaign.MessageText, cancellationToken))
            {
                paused++;
            }
        }

        foreach (var campaign in telegram)
        {
            if (await InspectCampaignAsync("Telegram", campaign.UserId, campaign.Id, campaign.Title, campaign.MessageText, cancellationToken))
            {
                paused++;
            }
        }

        if (paused > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return paused;
    }

    public async Task<bool> EnsureReadyAsync(string channel, int campaignId, CancellationToken cancellationToken = default)
    {
        if (string.Equals(channel, "Telegram", StringComparison.OrdinalIgnoreCase))
        {
            var campaign = await _context.TelegramBroadcastCampaigns.FirstOrDefaultAsync(item => item.Id == campaignId, cancellationToken);
            if (campaign is null || campaign.Status != TelegramBroadcastStatuses.Pending)
            {
                return false;
            }

            var paused = await InspectCampaignAsync("Telegram", campaign.UserId, campaign.Id, campaign.Title, campaign.MessageText, cancellationToken);
            if (paused)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return !paused;
        }

        var whats = await _context.WhatsAppBroadcastCampaigns.FirstOrDefaultAsync(item => item.Id == campaignId, cancellationToken);
        if (whats is null || whats.Status != WhatsAppBroadcastStatuses.Pending)
        {
            return false;
        }

        var blocked = await InspectCampaignAsync("WhatsApp", whats.UserId, whats.Id, whats.Title, whats.MessageText, cancellationToken);
        if (blocked)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return !blocked;
    }

    public async Task<PreFlightNotificationResponseDto> SubstituteAsync(
        int userId,
        int notificationId,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.PreFlightNotifications.FirstOrDefaultAsync(
            row => row.Id == notificationId && row.UserId == userId,
            cancellationToken);
        if (item is null)
        {
            return new PreFlightNotificationResponseDto { Status = false, Descricao = "Notificação não encontrada." };
        }

        var search = await _arbitrage.SearchAlternativesAsync(
            userId,
            new OfferArbitrageRequestDto
            {
                OriginalUrl = item.ProductUrl ?? string.Empty,
                ProductName = item.CampaignTitle,
                ProductPrice = item.CurrentPrice ?? item.OriginalPrice
            },
            cancellationToken);

        var best = search.Suggestions
            .Where(row => !string.IsNullOrWhiteSpace(row.AffiliateUrl) && row.Price is > 0)
            .OrderBy(row => row.Price)
            .FirstOrDefault();
        if (best is null)
        {
            var listed = await ListAsync(userId, cancellationToken);
            listed.Status = false;
            listed.Descricao = search.Descricao is { Length: > 0 }
                ? search.Descricao
                : "Nenhum preço menor encontrado nas lojas concorrentes.";
            return listed;
        }

        var replacement = best.AffiliateUrl!;
        if (!await ReplaceCampaignLinkAsync(item.Channel, item.CampaignId, userId, replacement, cancellationToken))
        {
            return new PreFlightNotificationResponseDto { Status = false, Descricao = "Não foi possível atualizar o agendamento pausado." };
        }

        item.SubstituteUrl = replacement.Length <= 2048 ? replacement : replacement[..2048];
        item.SubstitutePrice = best.Price;
        item.SubstituteName = best.ProductName;
        item.IsResolved = true;
        item.ResolvedAt = DateTime.UtcNow;
        item.Touch();
        await _context.SaveChangesAsync(cancellationToken);

        var response = await ListAsync(userId, cancellationToken);
        response.Descricao = $"Link substituído pelo menor preço ({PreFlightRules.FormatMoney(best.Price)}). O disparo voltou para a fila.";
        return response;
    }

    private async Task<bool> InspectCampaignAsync(
        string channel,
        int userId,
        int campaignId,
        string title,
        string? message,
        CancellationToken cancellationToken)
    {
        var urls = OfferAttributionRules.ExtractUrls(message);
        if (urls.Count == 0)
        {
            return false;
        }

        var already = await _context.PreFlightNotifications.AnyAsync(
            item => item.UserId == userId
                && item.CampaignId == campaignId
                && item.Channel == channel
                && !item.IsResolved,
            cancellationToken);
        if (already)
        {
            await PauseCampaignAsync(channel, campaignId, cancellationToken);
            return true;
        }

        var coupon = AffiliateMiningRules.ExtractCoupon(message);
        foreach (var url in urls)
        {
            try
            {
                var expected = PreFlightRules.ExtractExpectedPrice(message, url);
                var result = await _validation.ValidateAsync(url, expected, cancellationToken);
                string? alertType = null;
                if (!result.IsAvailable || result.Status == GroupOfferStatuses.Esgotado)
                {
                    alertType = PreFlightRules.AlertSoldOut;
                }
                else if (PreFlightRules.IsPriceIncrease(expected, result.Price))
                {
                    alertType = PreFlightRules.AlertPriceUp;
                }
                else if (!string.IsNullOrWhiteSpace(coupon))
                {
                    var page = await _validation.ValidateProductPageStatusAsync(url, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(page.Html)
                        && !page.Html.Contains(coupon, StringComparison.OrdinalIgnoreCase))
                    {
                        alertType = PreFlightRules.AlertCoupon;
                    }
                }

                if (alertType is null)
                {
                    continue;
                }

                var text = PreFlightRules.BuildPauseMessage(alertType, expected, result.Price, coupon);
                _context.PreFlightNotifications.Add(new PreFlightNotification
                {
                    UserId = userId,
                    Channel = channel,
                    CampaignId = campaignId,
                    CampaignTitle = title.Length <= 128 ? title : title[..128],
                    ProductUrl = url.Length <= 1000 ? url : url[..1000],
                    CouponCode = coupon,
                    AlertType = alertType,
                    Message = text,
                    OriginalPrice = expected,
                    CurrentPrice = result.Price
                });
                await PauseCampaignAsync(channel, campaignId, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Pre-flight falhou ao validar. Url={Url}", url);
            }
        }

        return false;
    }

    private async Task PauseCampaignAsync(string channel, int campaignId, CancellationToken cancellationToken)
    {
        if (string.Equals(channel, "Telegram", StringComparison.OrdinalIgnoreCase))
        {
            var campaign = await _context.TelegramBroadcastCampaigns.FirstOrDefaultAsync(item => item.Id == campaignId, cancellationToken);
            if (campaign is null || campaign.Status != TelegramBroadcastStatuses.Pending)
            {
                return;
            }

            campaign.Status = TelegramBroadcastStatuses.Paused;
            campaign.Touch();
            return;
        }

        var whats = await _context.WhatsAppBroadcastCampaigns.FirstOrDefaultAsync(item => item.Id == campaignId, cancellationToken);
        if (whats is null || whats.Status != WhatsAppBroadcastStatuses.Pending)
        {
            return;
        }

        whats.Status = WhatsAppBroadcastStatuses.Paused;
        whats.Touch();
    }

    private async Task<bool> ReplaceCampaignLinkAsync(
        string channel,
        int campaignId,
        int userId,
        string replacement,
        CancellationToken cancellationToken)
    {
        if (string.Equals(channel, "Telegram", StringComparison.OrdinalIgnoreCase))
        {
            var campaign = await _context.TelegramBroadcastCampaigns.FirstOrDefaultAsync(
                item => item.Id == campaignId && item.UserId == userId,
                cancellationToken);
            if (campaign is null)
            {
                return false;
            }

            campaign.MessageText = PreFlightRules.ReplaceFirstUrl(campaign.MessageText, replacement);
            campaign.Status = TelegramBroadcastStatuses.Pending;
            campaign.Touch();
            return true;
        }

        var whats = await _context.WhatsAppBroadcastCampaigns.FirstOrDefaultAsync(
            item => item.Id == campaignId && item.UserId == userId,
            cancellationToken);
        if (whats is null)
        {
            return false;
        }

        whats.MessageText = PreFlightRules.ReplaceFirstUrl(whats.MessageText, replacement);
        whats.Status = WhatsAppBroadcastStatuses.Pending;
        whats.Touch();
        return true;
    }

    private static PreFlightNotificationDto Map(PreFlightNotification item) =>
        new()
        {
            Id = item.Id,
            Channel = item.Channel,
            CampaignId = item.CampaignId,
            CampaignTitle = item.CampaignTitle,
            ProductUrl = item.ProductUrl,
            CouponCode = item.CouponCode,
            AlertType = item.AlertType,
            Message = item.Message,
            OriginalPrice = item.OriginalPrice,
            CurrentPrice = item.CurrentPrice,
            SubstituteUrl = item.SubstituteUrl,
            SubstitutePrice = item.SubstitutePrice,
            SubstituteName = item.SubstituteName,
            IsResolved = item.IsResolved,
            CreatedAt = item.CreatedAt
        };
}

public sealed class PreFlightEngine : IPreFlightEngine
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PreFlightEngine> _logger;

    public PreFlightEngine(IServiceScopeFactory scopeFactory, ILogger<PreFlightEngine> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RunCycleAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPreFlightService>();
        try
        {
            await service.InspectUpcomingAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ciclo de pre-flight falhou.");
        }
    }
}

public sealed class PreFlightHost
{
    private readonly IPreFlightEngine _engine;
    private readonly ILogger<PreFlightHost> _logger;

    public PreFlightHost(IPreFlightEngine engine, ILogger<PreFlightHost> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public async Task RunForeverAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _engine.RunCycleAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Worker de pre-flight falhou.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}

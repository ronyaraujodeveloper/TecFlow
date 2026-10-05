using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Database.Entity;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class EvergreenLibraryService : IEvergreenLibraryService
{
    private readonly AppDbContext _context;

    public EvergreenLibraryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EvergreenLibraryResponseDto> ListAsync(int userId, CancellationToken cancellationToken = default)
    {
        var items = await _context.EvergreenOffers
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.IsChampion)
            .ThenByDescending(item => item.ChampionScore)
            .Take(60)
            .ToListAsync(cancellationToken);

        return new EvergreenLibraryResponseDto
        {
            Status = true,
            Items = items.Select(Map).ToList()
        };
    }

    public async Task<EvergreenLibraryResponseDto> RefreshAsync(int userId, CancellationToken cancellationToken = default)
    {
        var links = await _context.ShortAffiliateLinks
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.IsActive)
            .OrderByDescending(item => item.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        var ids = links.Select(item => item.AffiliateLinkId).ToList();
        var clicks = await _context.LinkClickLogs
            .AsNoTracking()
            .Where(item => ids.Contains(item.AffiliateLinkId) && item.EventKind == LinkClickLog.EventKindClick)
            .GroupBy(item => item.AffiliateLinkId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        foreach (var link in links)
        {
            var count = clicks.GetValueOrDefault(link.AffiliateLinkId);
            if (count < 2 && string.IsNullOrWhiteSpace(link.ProductName))
            {
                continue;
            }

            var existing = await _context.EvergreenOffers.FirstOrDefaultAsync(
                item => item.UserId == userId && item.AffiliateUrl == link.AffiliateUrl,
                cancellationToken);
            var score = count * 10 + (link.ProductPrice is > 0 ? 5 : 0);
            if (existing is null)
            {
                _context.EvergreenOffers.Add(new EvergreenOffer
                {
                    UserId = userId,
                    ProductName = string.IsNullOrWhiteSpace(link.ProductName) ? "Oferta campeã" : link.ProductName,
                    ProductImageUrl = link.ProductImageUrl,
                    AffiliateUrl = link.AffiliateUrl,
                    OriginalUrl = link.OriginalUrl,
                    PlatformType = link.PlatformType,
                    PlatformName = link.PlatformType.ToString(),
                    ClickCount = count,
                    ChampionScore = score,
                    IsChampion = count >= 3
                });
            }
            else
            {
                existing.ClickCount = count;
                existing.ChampionScore = score;
                existing.IsChampion = count >= 3;
                existing.ProductImageUrl = link.ProductImageUrl ?? existing.ProductImageUrl;
                existing.Touch();
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await ListAsync(userId, cancellationToken);
    }

    public async Task<EvergreenLibraryResponseDto> RecycleAsync(int userId, CancellationToken cancellationToken = default)
    {
        await RefreshAsync(userId, cancellationToken);
        var horizon = DateTime.UtcNow.AddHours(6);
        var hasPending = await _context.WhatsAppBroadcastCampaigns.AnyAsync(
            item => item.UserId == userId
                && item.Status == WhatsAppBroadcastStatuses.Pending
                && item.ScheduledAt <= horizon,
            cancellationToken);
        var champion = await _context.EvergreenOffers
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.ChampionScore)
            .FirstOrDefaultAsync(cancellationToken);
        if (champion is null)
        {
            return new EvergreenLibraryResponseDto { Status = false, Descricao = "Ainda não há ofertas campeãs para reciclar." };
        }

        if (!hasPending)
        {
            var group = await _context.WhatsAppGroups
                .AsNoTracking()
                .Where(item => item.UserId == userId && item.IsActive)
                .Select(item => item.Jid)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(group))
            {
                _context.WhatsAppBroadcastCampaigns.Add(new WhatsAppBroadcastCampaign
                {
                    UserId = userId,
                    Title = "Reciclagem evergreen",
                    MessageText = WhatsAppBroadcastRules.ComposeDispatchMessage(champion.ProductName, champion.AffiliateUrl),
                    ImageUrl = champion.ProductImageUrl,
                    TargetGroupJidsJson = WhatsAppBroadcastRules.SerializeJids([group]),
                    ScheduledAt = DateTime.UtcNow.AddMinutes(30),
                    Status = WhatsAppBroadcastStatuses.Pending
                });
            }
        }

        champion.LastUsedAt = DateTime.UtcNow;
        champion.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        var list = await ListAsync(userId, cancellationToken);
        list.Descricao = hasPending
            ? "Há disparos no intervalo. A campeã foi marcada como usada."
            : "Intervalo vazio preenchido com uma oferta campeã (agendada em 30 min).";
        return list;
    }

    private static EvergreenOfferDto Map(EvergreenOffer item) =>
        new()
        {
            Id = item.Id,
            ProductName = item.ProductName,
            ProductImageUrl = item.ProductImageUrl,
            AffiliateUrl = item.AffiliateUrl,
            OriginalUrl = item.OriginalUrl,
            PlatformType = item.PlatformType,
            PlatformName = item.PlatformName,
            ClickCount = item.ClickCount,
            ChampionScore = item.ChampionScore,
            LastUsedAt = item.LastUsedAt,
            IsChampion = item.IsChampion
        };
}

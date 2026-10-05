using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class OfferRadarService : IOfferRadarService
{
    private readonly AppDbContext _context;
    private readonly IAffiliateMiningProfileService _profiles;

    public OfferRadarService(AppDbContext context, IAffiliateMiningProfileService profiles)
    {
        _context = context;
        _profiles = profiles;
    }

    public async Task<OfferRadarResponseDto> ListAsync(
        int userId,
        int skip = 0,
        int take = 30,
        CancellationToken cancellationToken = default)
    {
        var resolvedSkip = AffiliateMiningRules.ResolveSkip(skip);
        var resolvedTake = AffiliateMiningRules.ResolveTake(take);
        var profile = (await _profiles.GetAsync(userId, cancellationToken)).Profile
            ?? new AffiliateMiningProfileDto();
        var query = _context.OfferRadarItems
            .AsNoTracking()
            .Where(item => item.UserId == userId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.AttractivenessScore)
            .ThenByDescending(item => item.ReceivedAt)
            .Skip(resolvedSkip)
            .Take(resolvedTake)
            .ToListAsync(cancellationToken);

        return new OfferRadarResponseDto
        {
            Status = true,
            Items = items.Select(Map).ToList(),
            Total = total,
            Skip = resolvedSkip,
            Take = resolvedTake,
            AutoPilotEnabled = profile.AutoPilotEnabled
        };
    }

    public async Task<OfferRadarResponseDto> ScheduleAsync(
        int userId,
        int itemId,
        string? channel,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.OfferRadarItems
            .FirstOrDefaultAsync(row => row.Id == itemId && row.UserId == userId, cancellationToken);
        if (item is null)
        {
            return new OfferRadarResponseDto { Status = false, Descricao = "Oferta do radar não encontrada." };
        }

        var target = string.Equals(channel, "Telegram", StringComparison.OrdinalIgnoreCase)
            ? "/integracoes/telegram/agendador"
            : "/integracoes/whatsapp/agendador";
        var link = Uri.EscapeDataString(item.AffiliateUrl ?? item.OriginalUrl);
        var title = Uri.EscapeDataString(item.ProductName);
        var image = Uri.EscapeDataString(item.ProductImageUrl ?? string.Empty);
        var list = await ListAsync(userId, cancellationToken: cancellationToken);
        list.ScheduleUrl = $"{target}?cloneLink={link}&cloneTitle={title}&cloneImage={image}&cloneMessage={title}";
        list.Descricao = "Oferta pronta para o agendador.";
        return list;
    }

    public async Task<OfferRadarResponseDto> QueueAutoPilotAsync(
        int userId,
        int itemId,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.OfferRadarItems
            .FirstOrDefaultAsync(row => row.Id == itemId && row.UserId == userId, cancellationToken);
        if (item is null)
        {
            return new OfferRadarResponseDto { Status = false, Descricao = "Oferta do radar não encontrada." };
        }

        item.IsAutoQueued = true;
        item.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        var list = await ListAsync(userId, cancellationToken: cancellationToken);
        list.Descricao = "Item enviado ao piloto automático.";
        return list;
    }

    private static OfferRadarItemDto Map(OfferRadarItem item) =>
        new()
        {
            Id = item.Id,
            ProductName = item.ProductName,
            ProductImageUrl = item.ProductImageUrl,
            OriginalUrl = item.OriginalUrl,
            AffiliateUrl = item.AffiliateUrl,
            PlatformType = item.PlatformType,
            PlatformName = item.PlatformName,
            Price = item.Price,
            ComparedPrice = item.ComparedPrice,
            CouponCode = item.CouponCode,
            AttractivenessScore = item.AttractivenessScore,
            Source = item.Source,
            SourceLabel = OfferRadarSources.ToUiLabel(item.Source),
            IsAutoQueued = item.IsAutoQueued,
            ReceivedAt = item.ReceivedAt
        };
}

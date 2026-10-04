using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Business.Service.PublicPages;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class MonitoredGroupService : IMonitoredGroupService
{
    private readonly AppDbContext _context;
    private readonly IWhatsAppBroadcastService _whatsAppBroadcasts;
    private readonly IOfferValidationService _validation;
    private readonly IAffiliateLinkGenerationService _generation;
    private readonly IMarketplaceAccountRepository _marketplaceAccounts;
    private readonly PlatformLinkResolver _platformLinkResolver;

    public MonitoredGroupService(
        AppDbContext context,
        IWhatsAppBroadcastService whatsAppBroadcasts,
        IOfferValidationService validation,
        IAffiliateLinkGenerationService generation,
        IMarketplaceAccountRepository marketplaceAccounts,
        PlatformLinkResolver platformLinkResolver)
    {
        _context = context;
        _whatsAppBroadcasts = whatsAppBroadcasts;
        _validation = validation;
        _generation = generation;
        _marketplaceAccounts = marketplaceAccounts;
        _platformLinkResolver = platformLinkResolver;
    }

    public async Task<MonitoredGroupsResponseDto> SyncAsync(int userId, CancellationToken cancellationToken = default)
    {
        var sync = await _whatsAppBroadcasts.SyncGroupsAsync(userId, cancellationToken);
        var list = await ListAsync(userId, 24, null, cancellationToken);
        list.Status = sync.Status;
        list.Descricao = sync.Status
            ? "Grupos do WhatsApp sincronizados. Ofertas do Telegram entram pelo webhook dos canais monitorados."
            : sync.Descricao;
        return list;
    }

    public async Task<MonitoredGroupsResponseDto> ListAsync(
        int userId,
        int lookbackHours,
        string? groupKey,
        CancellationToken cancellationToken = default)
    {
        var hours = GroupOfferCaptureRules.ResolveLookbackHours(lookbackHours);
        var since = DateTime.UtcNow.AddHours(-hours);

        var groups = await BuildGroupsAsync(userId, cancellationToken);
        var query = _context.GroupCapturedMessages
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.ReceivedAt >= since);
        if (!string.IsNullOrWhiteSpace(groupKey))
        {
            query = query.Where(item => item.GroupKey == groupKey);
        }

        var offers = await query
            .OrderByDescending(item => item.ReceivedAt)
            .Take(120)
            .ToListAsync(cancellationToken);

        return new MonitoredGroupsResponseDto
        {
            Status = true,
            Groups = groups,
            Offers = offers.Select(MapOffer).ToList()
        };
    }

    public async Task<MonitoredGroupsResponseDto> ValidateAsync(
        int userId,
        int offerId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.GroupCapturedMessages
            .FirstOrDefaultAsync(item => item.Id == offerId && item.UserId == userId, cancellationToken);
        if (entity is null)
        {
            return new MonitoredGroupsResponseDto { Status = false, Descricao = "Oferta não encontrada." };
        }

        var result = await _validation.ValidateAsync(entity.OriginalUrl, entity.ExtractedPrice, cancellationToken);
        entity.OfferStatus = result.Status;
        entity.ValidatedPrice = result.Price;
        entity.LastValidatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(result.ImageUrl))
        {
            entity.ProductImageUrl = result.ImageUrl;
        }

        if (!string.IsNullOrWhiteSpace(result.ProductName))
        {
            entity.ProductName = result.ProductName;
        }

        if (result.Platform is { } platform)
        {
            entity.PlatformType = platform;
            entity.PlatformName = platform.ToString();
        }

        entity.Touch();
        await _context.SaveChangesAsync(cancellationToken);

        var list = await ListAsync(userId, 24, entity.GroupKey, cancellationToken);
        list.Descricao = $"Status atualizado: {GroupOfferStatuses.ToUiLabel(entity.OfferStatus)}.";
        return list;
    }

    public async Task<MonitoredGroupsResponseDto> CloneAsync(
        int userId,
        int offerId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.GroupCapturedMessages
            .FirstOrDefaultAsync(item => item.Id == offerId && item.UserId == userId, cancellationToken);
        if (entity is null)
        {
            return new MonitoredGroupsResponseDto { Status = false, Descricao = "Oferta não encontrada." };
        }

        var validation = await _validation.ValidateAsync(entity.OriginalUrl, entity.ExtractedPrice, cancellationToken);
        entity.OfferStatus = validation.Status;
        entity.ValidatedPrice = validation.Price;
        entity.LastValidatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(validation.ImageUrl))
        {
            entity.ProductImageUrl = validation.ImageUrl;
        }

        if (!string.IsNullOrWhiteSpace(validation.ProductName))
        {
            entity.ProductName = validation.ProductName;
        }

        entity.Touch();
        await _context.SaveChangesAsync(cancellationToken);

        if (entity.OfferStatus == GroupOfferStatuses.Esgotado)
        {
            return new MonitoredGroupsResponseDto
            {
                Status = false,
                Descricao = "O anúncio parece esgotado ou indisponível. Confira o link original antes de clonar."
            };
        }

        var affiliateUrl = await ConvertUrlAsync(userId, entity.OriginalUrl, cancellationToken);
        if (string.IsNullOrWhiteSpace(affiliateUrl))
        {
            return new MonitoredGroupsResponseDto
            {
                Status = false,
                Descricao = "Não foi possível converter o link. Cadastre uma loja ativa da mesma plataforma."
            };
        }

        var title = string.IsNullOrWhiteSpace(entity.ProductName) ? "Oferta clonada" : entity.ProductName!;
        var message = title;
        if (entity.ValidatedPrice is > 0 || entity.ExtractedPrice is > 0)
        {
            message += " por " + ProductMetadataHtmlParser.FormatBrl(entity.ValidatedPrice ?? entity.ExtractedPrice);
        }

        var image = entity.ProductImageUrl;
        var redirect =
            "/integracoes/whatsapp/agendador?cloneTitle=" + Uri.EscapeDataString(title)
            + "&cloneMessage=" + Uri.EscapeDataString(message)
            + "&cloneLink=" + Uri.EscapeDataString(affiliateUrl)
            + (string.IsNullOrWhiteSpace(image) ? string.Empty : "&cloneImage=" + Uri.EscapeDataString(image));

        return new MonitoredGroupsResponseDto
        {
            Status = true,
            Descricao = "Link convertido. Abrindo o agendador WhatsApp.",
            Clone = new CloneMonitoredOfferResultDto
            {
                Status = true,
                AffiliateUrl = affiliateUrl,
                ImageUrl = image,
                Title = title,
                Message = message,
                OfferStatus = entity.OfferStatus,
                RedirectUrl = redirect
            }
        };
    }

    private async Task<List<MonitoredGroupDto>> BuildGroupsAsync(int userId, CancellationToken cancellationToken)
    {
        var whats = await _context.WhatsAppGroups
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new MonitoredGroupDto
            {
                GroupKey = GroupOfferCaptureRules.BuildGroupKey(GroupOfferCaptureRules.WhatsAppChannel, item.Jid),
                Name = item.Name,
                Channel = GroupOfferCaptureRules.WhatsAppChannel
            })
            .ToListAsync(cancellationToken);

        var captured = await _context.GroupCapturedMessages
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => new { item.GroupKey, item.GroupName, item.Channel })
            .ToListAsync(cancellationToken);
        var capturedGroups = captured
            .GroupBy(item => item.GroupKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => new MonitoredGroupDto
            {
                GroupKey = group.Key,
                Name = group.First().GroupName,
                Channel = group.First().Channel
            })
            .ToList();

        return whats
            .Concat(capturedGroups)
            .GroupBy(item => item.GroupKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.Name)
            .ToList();
    }

    private async Task<string?> ConvertUrlAsync(int userId, string originalUrl, CancellationToken cancellationToken)
    {
        var (_, resolvedUrl) = await _platformLinkResolver.ResolveFromInputAsync(originalUrl, cancellationToken);
        if (!UrlUnshortenerService.TryDetectMarketplace(resolvedUrl, out var platform)
            && !UniversalLinkResolverEngine.TryMapDomainToPlatform(resolvedUrl, out platform))
        {
            return null;
        }

        var accounts = await _marketplaceAccounts.ListByUserIdAsync(userId.ToString(), cancellationToken);
        var account = PublicConverterRules.FirstActiveForPlatform(accounts, platform);
        if (account is null)
        {
            return null;
        }

        var request = new GerarLinkAfiliadoDto
        {
            OriginalUrl = originalUrl,
            StoreId = IntegracaoLojaScopeHelper.EncodeStoreScope(account.Id),
            StoreIds = [IntegracaoLojaScopeHelper.EncodeStoreScope(account.Id)],
            TenantId = account.TenantId,
            ShopId = account.ShopId,
            Source = "GroupClone"
        };

        var result = await _generation.GenerateAsync(request, userId, cancellationToken);
        if (!result.Success && !result.HasConvertedLink)
        {
            return null;
        }

        return FirstNonEmpty(result.AffiliateUrl, result.ResolvedShortUrl, result.ConvertedUrl);
    }

    private static GroupCapturedOfferDto MapOffer(GroupCapturedMessage item) =>
        new()
        {
            Id = item.Id,
            Channel = item.Channel,
            GroupKey = item.GroupKey,
            GroupName = item.GroupName,
            ProductName = item.ProductName,
            ExtractedPrice = item.ValidatedPrice ?? item.ExtractedPrice,
            ValidatedPrice = item.ValidatedPrice,
            ProductImageUrl = item.ProductImageUrl ?? item.MediaUrl,
            OriginalUrl = item.OriginalUrl,
            PlatformType = item.PlatformType,
            PlatformName = item.PlatformName,
            OfferStatus = item.OfferStatus,
            OfferStatusLabel = GroupOfferStatuses.ToUiLabel(item.OfferStatus),
            ReceivedAt = item.ReceivedAt
        };

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
}

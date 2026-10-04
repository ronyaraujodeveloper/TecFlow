using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ITelegramBroadcastService _telegramBroadcasts;
    private readonly IOfferValidationService _validation;
    private readonly IAffiliateLinkGenerationService _generation;
    private readonly IMarketplaceAccountRepository _marketplaceAccounts;
    private readonly PlatformLinkResolver _platformLinkResolver;
    private readonly ILogger<MonitoredGroupService> _logger;

    public MonitoredGroupService(
        AppDbContext context,
        IWhatsAppBroadcastService whatsAppBroadcasts,
        ITelegramBroadcastService telegramBroadcasts,
        IOfferValidationService validation,
        IAffiliateLinkGenerationService generation,
        IMarketplaceAccountRepository marketplaceAccounts,
        PlatformLinkResolver platformLinkResolver,
        ILogger<MonitoredGroupService> logger)
    {
        _context = context;
        _whatsAppBroadcasts = whatsAppBroadcasts;
        _telegramBroadcasts = telegramBroadcasts;
        _validation = validation;
        _generation = generation;
        _marketplaceAccounts = marketplaceAccounts;
        _platformLinkResolver = platformLinkResolver;
        _logger = logger;
    }

    public async Task<MonitoredGroupsResponseDto> SyncAsync(
        int userId,
        string? channel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notes = new List<string>();
            var normalized = GroupOfferCaptureRules.NormalizeChannel(channel);
            var whatsOk = false;
            var telegramOk = false;
            if (normalized is null || normalized == GroupOfferCaptureRules.WhatsAppChannel)
            {
                whatsOk = await SyncWhatsAppIsolatedAsync(userId, notes, cancellationToken);
            }

            if (normalized is null || normalized == GroupOfferCaptureRules.TelegramChannel)
            {
                telegramOk = await SyncTelegramIsolatedAsync(userId, notes, cancellationToken);
            }

            var list = await ListAsync(userId, 24, null, normalized, cancellationToken);
            list.Status = whatsOk || telegramOk || notes.Count == 0;
            list.Descricao = notes.Count == 0
                ? "Grupos sincronizados."
                : string.Join(" ", notes);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar grupos monitorados");
            return new MonitoredGroupsResponseDto
            {
                Status = false,
                Descricao = string.IsNullOrWhiteSpace(ex.Message)
                    ? "Erro ao sincronizar grupos monitorados."
                    : ex.Message
            };
        }
    }

    public async Task<MonitoredGroupsResponseDto> ListAsync(
        int userId,
        int lookbackHours,
        string? groupKey,
        string? channel,
        CancellationToken cancellationToken = default)
    {
        var hours = GroupOfferCaptureRules.ResolveLookbackHours(lookbackHours);
        var since = DateTime.UtcNow.AddHours(-hours);
        var normalized = GroupOfferCaptureRules.NormalizeChannel(channel);

        try
        {
            var groups = await BuildGroupsAsync(userId, normalized, cancellationToken);
            var query = _context.GroupCapturedMessages
                .AsNoTracking()
                .Where(item => item.UserId == userId && item.ReceivedAt >= since);
            if (normalized is not null)
            {
                query = query.Where(item => item.Channel == normalized);
            }

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar grupos monitorados");
            return new MonitoredGroupsResponseDto
            {
                Status = false,
                Descricao = string.IsNullOrWhiteSpace(ex.Message)
                    ? "Erro ao listar grupos monitorados."
                    : ex.Message
            };
        }
    }

    public async Task<MonitoredGroupsResponseDto> ValidateAsync(
        int userId,
        int offerId,
        string? channel,
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

        var list = await ListAsync(
            userId,
            24,
            entity.GroupKey,
            channel ?? entity.Channel,
            cancellationToken);
        list.Descricao = $"Status atualizado: {GroupOfferStatuses.ToUiLabel(entity.OfferStatus)}.";
        return list;
    }

    public async Task<MonitoredGroupsResponseDto> CloneAsync(
        int userId,
        int offerId,
        string? channel,
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
        var targetChannel = GroupOfferCaptureRules.NormalizeChannel(channel)
            ?? GroupOfferCaptureRules.NormalizeChannel(entity.Channel)
            ?? GroupOfferCaptureRules.WhatsAppChannel;
        var scheduler = targetChannel == GroupOfferCaptureRules.TelegramChannel
            ? "/integracoes/telegram/agendador"
            : "/integracoes/whatsapp/agendador";
        var redirect =
            scheduler
            + "?cloneTitle=" + Uri.EscapeDataString(title)
            + "&cloneMessage=" + Uri.EscapeDataString(message)
            + "&cloneLink=" + Uri.EscapeDataString(affiliateUrl)
            + (string.IsNullOrWhiteSpace(image) ? string.Empty : "&cloneImage=" + Uri.EscapeDataString(image));

        return new MonitoredGroupsResponseDto
        {
            Status = true,
            Descricao = targetChannel == GroupOfferCaptureRules.TelegramChannel
                ? "Link convertido. Abrindo o agendador Telegram."
                : "Link convertido. Abrindo o agendador WhatsApp.",
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

    private async Task<List<MonitoredGroupDto>> BuildGroupsAsync(
        int userId,
        string? channel,
        CancellationToken cancellationToken)
    {
        var includeWhatsApp = channel is null || channel == GroupOfferCaptureRules.WhatsAppChannel;
        var includeTelegram = channel is null || channel == GroupOfferCaptureRules.TelegramChannel;

        var whats = includeWhatsApp
            ? await _context.WhatsAppGroups
                .AsNoTracking()
                .Where(item => item.UserId == userId && item.IsActive)
                .OrderBy(item => item.Name)
                .Select(item => new MonitoredGroupDto
                {
                    GroupKey = GroupOfferCaptureRules.BuildGroupKey(GroupOfferCaptureRules.WhatsAppChannel, item.Jid),
                    Name = item.Name,
                    Channel = GroupOfferCaptureRules.WhatsAppChannel
                })
                .ToListAsync(cancellationToken)
            : [];

        var telegram = includeTelegram
            ? await _context.TelegramGroups
                .AsNoTracking()
                .Where(item => item.UserId == userId && item.IsActive)
                .OrderBy(item => item.Name)
                .Select(item => new MonitoredGroupDto
                {
                    GroupKey = GroupOfferCaptureRules.BuildGroupKey(GroupOfferCaptureRules.TelegramChannel, item.ChatId),
                    Name = item.Name,
                    Channel = GroupOfferCaptureRules.TelegramChannel
                })
                .ToListAsync(cancellationToken)
            : [];

        var capturedQuery = _context.GroupCapturedMessages
            .AsNoTracking()
            .Where(item => item.UserId == userId);
        if (channel is not null)
        {
            capturedQuery = capturedQuery.Where(item => item.Channel == channel);
        }

        var captured = await capturedQuery
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
            .Concat(telegram)
            .Concat(capturedGroups)
            .GroupBy(item => item.GroupKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.Name)
            .ToList();
    }

    private async Task<bool> SyncWhatsAppIsolatedAsync(
        int userId,
        ICollection<string> notes,
        CancellationToken cancellationToken)
    {
        try
        {
            var sync = await _whatsAppBroadcasts.SyncGroupsAsync(userId, cancellationToken);
            if (sync.Status)
            {
                notes.Add("WhatsApp sincronizado.");
                return true;
            }

            notes.Add(string.IsNullOrWhiteSpace(sync.Descricao)
                ? "WhatsApp não sincronizou."
                : $"WhatsApp: {sync.Descricao}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar grupos monitorados do WhatsApp (Evolution API)");
            notes.Add($"WhatsApp: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> SyncTelegramIsolatedAsync(
        int userId,
        ICollection<string> notes,
        CancellationToken cancellationToken)
    {
        try
        {
            var sync = await _telegramBroadcasts.SyncChannelsAsync(userId, cancellationToken);
            if (sync.Status)
            {
                notes.Add("Telegram sincronizado.");
                return true;
            }

            notes.Add(string.IsNullOrWhiteSpace(sync.Descricao)
                ? "Telegram não sincronizou."
                : $"Telegram: {sync.Descricao}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar grupos monitorados do Telegram (Bot API). UserBot não interrompe esta etapa.");
            notes.Add($"Telegram: {ex.Message}");
            return false;
        }
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

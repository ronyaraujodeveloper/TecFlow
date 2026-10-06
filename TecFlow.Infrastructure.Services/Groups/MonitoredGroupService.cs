using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Business.Service.Telegram;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Infrastructure.Services.Telegram;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class MonitoredGroupService : IMonitoredGroupService
{
    private readonly AppDbContext _context;
    private readonly IWhatsAppBroadcastService _whatsAppBroadcasts;
    private readonly ITelegramBroadcastService _telegramBroadcasts;
    private readonly TelegramUserMonitorHost _userBotHost;
    private readonly IOfferValidationService _validation;
    private readonly IAffiliateLinkConverterService _converter;
    private readonly IGroupCapturedMessagesService _capturedMessages;
    private readonly ILogger<MonitoredGroupService> _logger;

    public MonitoredGroupService(
        AppDbContext context,
        IWhatsAppBroadcastService whatsAppBroadcasts,
        ITelegramBroadcastService telegramBroadcasts,
        TelegramUserMonitorHost userBotHost,
        IOfferValidationService validation,
        IAffiliateLinkConverterService converter,
        IGroupCapturedMessagesService capturedMessages,
        ILogger<MonitoredGroupService> logger)
    {
        _context = context;
        _whatsAppBroadcasts = whatsAppBroadcasts;
        _telegramBroadcasts = telegramBroadcasts;
        _userBotHost = userBotHost;
        _validation = validation;
        _converter = converter;
        _capturedMessages = capturedMessages;
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

            var list = await ListAsync(userId, 24, null, normalized, skip: 0, take: GroupOfferCaptureRules.OffersPageSize, ignored: false, cancellationToken);
            list.Status = whatsOk || telegramOk || notes.Count == 0;
            list.Descricao = notes.Exists(item => item == MonitoredGroupSyncRules.BackgroundStartedMessage)
                ? MonitoredGroupSyncRules.BackgroundStartedMessage
                : notes.Count == 0
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
        int skip = 0,
        int take = 50,
        bool ignored = false,
        CancellationToken cancellationToken = default)
    {
        var hours = GroupOfferCaptureRules.ResolveLookbackHours(lookbackHours);
        var since = DateTime.UtcNow.AddHours(-hours);
        var normalized = GroupOfferCaptureRules.NormalizeChannel(channel);
        var resolvedSkip = GroupOfferCaptureRules.ResolveOffersSkip(skip);
        var resolvedTake = GroupOfferCaptureRules.ResolveOffersTake(take);

        try
        {
            var groups = await BuildGroupsAsync(userId, normalized, cancellationToken);
            var platforms = await _capturedMessages.ListActivePlatformsAsync(userId, cancellationToken);
            var query = _context.GroupCapturedMessages
                .AsNoTracking()
                .Where(item => item.UserId == userId && item.ReceivedAt >= since);
            query = _capturedMessages.ApplyRelevanceFilter(query, platforms, ignored);
            if (normalized is not null)
            {
                query = query.Where(item => item.Channel == normalized);
            }

            if (!string.IsNullOrWhiteSpace(groupKey))
            {
                query = query.Where(item => item.GroupKey == groupKey);
            }

            var total = await query.CountAsync(cancellationToken);
            var offers = await query
                .OrderByDescending(item => item.ReceivedAt)
                .Skip(resolvedSkip)
                .Take(resolvedTake)
                .ToListAsync(cancellationToken);

            return new MonitoredGroupsResponseDto
            {
                Status = true,
                Groups = groups,
                Offers = offers.Select(MapOffer).ToList(),
                TotalOffers = total,
                Skip = resolvedSkip,
                Take = resolvedTake,
                Ignored = ignored
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
        ApplyValidation(entity, result);
        await _context.SaveChangesAsync(cancellationToken);

        var list = await ListAsync(
            userId,
            24,
            entity.GroupKey,
            channel ?? entity.Channel,
            skip: 0,
            take: GroupOfferCaptureRules.OffersPageSize,
            ignored: false,
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
        ApplyValidation(entity, validation);
        await _context.SaveChangesAsync(cancellationToken);

        if (entity.OfferStatus == GroupOfferStatuses.Esgotado)
        {
            return new MonitoredGroupsResponseDto
            {
                Status = false,
                Descricao = "O anúncio parece esgotado ou indisponível. Confira o link original antes de clonar."
            };
        }

        var converted = await _converter.ConvertAsync(
            userId,
            entity.OriginalUrl,
            entity.GroupName,
            cancellationToken);
        if (!converted.Status || converted.Data is null || string.IsNullOrWhiteSpace(converted.Data.AffiliateUrl))
        {
            return new MonitoredGroupsResponseDto
            {
                Status = false,
                Descricao = string.IsNullOrWhiteSpace(converted.Descricao)
                    ? CloneOfferRules.MissingStoreMessage
                    : converted.Descricao
            };
        }

        var title = FirstNonEmpty(converted.Data.Title, entity.ProductName) ?? "Oferta clonada";
        var price = converted.Data.Price ?? entity.ValidatedPrice ?? entity.ExtractedPrice;
        var message = title;
        if (price is > 0)
        {
            message += " por " + ProductMetadataHtmlParser.FormatBrl(price);
        }

        var image = FirstNonEmpty(converted.Data.ImageUrl, entity.ProductImageUrl);
        var affiliateUrl = converted.Data.AffiliateUrl;
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
            Descricao = CloneOfferRules.SuccessToast,
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

    public async Task<MonitoredGroupsResponseDto> SetIgnoredAsync(
        int userId,
        int offerId,
        bool ignored,
        string? channel,
        CancellationToken cancellationToken = default)
    {
        var updated = await _capturedMessages.SetIgnoredAsync(userId, offerId, ignored, cancellationToken);
        if (!updated)
        {
            return new MonitoredGroupsResponseDto { Status = false, Descricao = "Oferta não encontrada." };
        }

        return new MonitoredGroupsResponseDto
        {
            Status = true,
            Descricao = ignored ? "Oferta marcada como sem interesse." : "Oferta restaurada no feed.",
            Ignored = ignored
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
        var botOk = false;
        try
        {
            var sync = await _telegramBroadcasts.SyncChannelsAsync(userId, cancellationToken);
            if (sync.Status)
            {
                notes.Add("Telegram sincronizado.");
                botOk = true;
            }
            else
            {
                notes.Add(string.IsNullOrWhiteSpace(sync.Descricao)
                    ? "Telegram (Bot API) não sincronizou."
                    : $"Telegram: {sync.Descricao}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar grupos monitorados do Telegram (Bot API). UserBot não interrompe esta etapa.");
            notes.Add($"Telegram: {ex.Message}");
        }

        try
        {
            if (_userBotHost.EnqueueCatchUp(userId))
            {
                notes.Add(MonitoredGroupSyncRules.BackgroundStartedMessage);
                return true;
            }

            notes.Add("Não foi possível enfileirar a varredura do histórico UserBot.");
            return botOk;
        }
        catch (Exception catchUpEx)
        {
            _logger.LogWarning(catchUpEx, "Falha ao enfileirar catch-up UserBot. UserId={UserId}", userId);
            notes.Add("A varredura do histórico UserBot falhou ao iniciar. Confira a sessão MTProto e sincronize de novo.");
            return botOk;
        }
    }


    private static GroupCapturedOfferDto MapOffer(GroupCapturedMessage item) =>
        new()
        {
            Id = item.Id,
            Channel = item.Channel,
            GroupKey = item.GroupKey,
            GroupName = item.GroupName,
            ProductName = item.ProductName,
            CouponCode = item.CouponCode,
            ExtractedPrice = item.ValidatedPrice ?? item.ExtractedPrice,
            ValidatedPrice = item.ValidatedPrice,
            ProductImageUrl = ProductImageStorageRules.ToWebRelativePath(item.ProductImageUrl ?? item.MediaUrl),
            OriginalUrl = item.OriginalUrl,
            PrimaryProductUrl = item.PrimaryProductUrl ?? item.OriginalUrl,
            PlatformType = item.PlatformType,
            PlatformName = item.PlatformName,
            OfferStatus = item.OfferStatus,
            OfferStatusLabel = GroupOfferStatuses.ToUiLabel(item.OfferStatus),
            ReceivedAt = item.ReceivedAt,
            HasDirectProductUrl = item.HasDirectProductUrl,
            IsIgnored = item.IsIgnored,
            IsAvailable = item.IsAvailable
        };

    private static void ApplyValidation(GroupCapturedMessage entity, OfferValidationResultDto result)
    {
        entity.OfferStatus = result.Status;
        entity.IsAvailable = result.IsAvailable;
        entity.ValidatedPrice = result.Price;
        entity.LastValidatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(result.ImageUrl))
        {
            entity.ProductImageUrl = ProductImageStorageRules.ToWebRelativePath(result.ImageUrl);
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
}

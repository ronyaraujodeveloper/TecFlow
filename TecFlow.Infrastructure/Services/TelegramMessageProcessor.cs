using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Business.Service.PublicPages;
using TecFlow.Business.Service.Security;
using TecFlow.Business.Service.Telegram;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services;

public sealed class TelegramMessageProcessor : ITelegramMessageProcessor
{
    private readonly AppDbContext _context;
    private readonly IMarketplaceAccountRepository _marketplaceAccounts;
    private readonly PlatformLinkResolver _platformLinkResolver;
    private readonly IAffiliateLinkGenerationService _generationService;
    private readonly ITelegramApiService _telegramApi;
    private readonly IGroupOfferCaptureService _groupCapture;
    private readonly ILogger<TelegramMessageProcessor> _logger;

    public TelegramMessageProcessor(
        AppDbContext context,
        IMarketplaceAccountRepository marketplaceAccounts,
        PlatformLinkResolver platformLinkResolver,
        IAffiliateLinkGenerationService generationService,
        ITelegramApiService telegramApi,
        IGroupOfferCaptureService groupCapture,
        ILogger<TelegramMessageProcessor> logger)
    {
        _context = context;
        _marketplaceAccounts = marketplaceAccounts;
        _platformLinkResolver = platformLinkResolver;
        _generationService = generationService;
        _telegramApi = telegramApi;
        _groupCapture = groupCapture;
        _logger = logger;
    }

    public async Task ProcessAsync(
        int? userId,
        JsonElement payload,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(TelegramBotRules.ReplyTimeoutMilliseconds));
        var token = timeout.Token;

        var incoming = TelegramBotRules.TryParseIncoming(payload);
        if (incoming is null
            || incoming.FromBot
            || string.IsNullOrWhiteSpace(incoming.ChatId))
        {
            return;
        }

        if (userId is not int currentUserId)
        {
            return;
        }

        var integration = await _context.TelegramIntegrations
            .AsNoTracking()
            .Where(item => item.IsActive && item.UserId == currentUserId)
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(token);
        if (integration is null || string.IsNullOrWhiteSpace(integration.Token))
        {
            return;
        }

        if (integration.UserId != currentUserId)
        {
            throw new UnauthorizedAccessException();
        }

        IntegrationOwnershipGuard.EnsureOwner(integration.UserId, currentUserId);

        if (TelegramBotRules.ShouldCaptureGroup(incoming))
        {
            await _groupCapture.CaptureAsync(
                new GroupOfferCaptureRequest
                {
                    UserId = currentUserId,
                    Channel = "Telegram",
                    GroupId = incoming.ChatId,
                    GroupName = string.IsNullOrWhiteSpace(incoming.ChatTitle) ? incoming.ChatId : incoming.ChatTitle,
                    ExternalMessageId = incoming.MessageId,
                    RawText = incoming.Text,
                    ReceivedAt = DateTime.UtcNow
                },
                cancellationToken);
        }

        if (TelegramBotRules.ShouldIgnore(incoming))
        {
            return;
        }

        var urls = TelegramBotRules.ExtractUrls(incoming.Text);
        if (urls.Count == 0)
        {
            return;
        }

        var converted = new List<string>();
        foreach (var url in urls)
        {
            token.ThrowIfCancellationRequested();
            var link = await ConvertUrlAsync(currentUserId, url, token);
            if (!string.IsNullOrWhiteSpace(link))
            {
                converted.Add(link);
            }
        }

        var reply = TelegramBotRules.FormatConvertedReply(converted);
        if (string.IsNullOrWhiteSpace(reply))
        {
            return;
        }

        try
        {
            await _telegramApi.SendTextMessageAsync(integration.Token, incoming.ChatId, reply, token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("SendTextMessageAsync Telegram cancelado após 2s. UserId={UserId}", currentUserId);
        }
    }

    private async Task<string?> ConvertUrlAsync(int userId, string originalUrl, CancellationToken cancellationToken)
    {
        try
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
                Source = TelegramBotRules.TelegramBotSource
            };

            var result = await _generationService.GenerateAsync(request, userId, cancellationToken);
            if (!result.Success && !result.HasConvertedLink)
            {
                return null;
            }

            return FirstNonEmpty(result.AffiliateUrl, result.ResolvedShortUrl, result.ConvertedUrl);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao converter URL do Telegram. UserId={UserId}", userId);
            return null;
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
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Business.Service.PublicPages;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services;

public sealed class WhatsAppMessageProcessor : IWhatsAppMessageProcessor
{
    private readonly AppDbContext _context;
    private readonly IMarketplaceAccountRepository _marketplaceAccounts;
    private readonly PlatformLinkResolver _platformLinkResolver;
    private readonly IAffiliateLinkGenerationService _generationService;
    private readonly IEvolutionApiService _evolution;
    private readonly ILogger<WhatsAppMessageProcessor> _logger;

    public WhatsAppMessageProcessor(
        AppDbContext context,
        IMarketplaceAccountRepository marketplaceAccounts,
        PlatformLinkResolver platformLinkResolver,
        IAffiliateLinkGenerationService generationService,
        IEvolutionApiService evolution,
        ILogger<WhatsAppMessageProcessor> logger)
    {
        _context = context;
        _marketplaceAccounts = marketplaceAccounts;
        _platformLinkResolver = platformLinkResolver;
        _generationService = generationService;
        _evolution = evolution;
        _logger = logger;
    }

    public async Task ProcessAsync(JsonElement payload, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(WhatsAppBotRules.ReplyTimeoutMilliseconds));
        var token = timeout.Token;

        var incoming = WhatsAppBotRules.TryParseIncoming(payload);
        if (incoming is null || WhatsAppBotRules.ShouldIgnore(incoming))
        {
            return;
        }

        var session = await _context.WhatsAppIntegrations
            .AsNoTracking()
            .Where(item => item.IsActive && item.InstanceName == incoming.InstanceName)
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(token);
        if (session is null)
        {
            return;
        }

        if (!WhatsAppBotRules.ShouldHandleChat(
            session.EnableAutoConvertBot,
            session.ReplyToPrivateMessages,
            session.ReplyToGroupMessages,
            incoming.IsGroup))
        {
            return;
        }

        var urls = WhatsAppBotRules.ExtractUrls(incoming.Text);
        if (urls.Count == 0)
        {
            return;
        }

        var converted = new List<string>();
        foreach (var url in urls)
        {
            token.ThrowIfCancellationRequested();
            var link = await ConvertUrlAsync(session.UserId, url, token);
            if (!string.IsNullOrWhiteSpace(link))
            {
                converted.Add(link);
            }
        }

        var reply = WhatsAppBotRules.FormatConvertedReply(converted);
        if (string.IsNullOrWhiteSpace(reply))
        {
            return;
        }

        try
        {
            await _evolution.SendTextMessageAsync(session.InstanceName, incoming.RemoteJid, reply, token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "sendText WhatsApp cancelado após o limite de 3s. Instance={Instance}",
                session.InstanceName);
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
                Source = WhatsAppBotRules.WhatsAppBotSource
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
            _logger.LogWarning(ex, "Falha ao converter URL do WhatsApp. UserId={UserId}", userId);
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

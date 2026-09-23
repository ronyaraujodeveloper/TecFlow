using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Configuration;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Database.Entity;
using TecFlow.Infrastructure.Services.Repositories;
using TecFlow.Util.Text;

namespace TecFlow.Infrastructure.Services.ShortLinks;

public sealed class ShortLinkService : IShortLinkService
{
    private const int MaxUniqueAttempts = 8;

    private readonly IShortAffiliateLinkRepository _shortLinkRepository;
    private readonly AppDbContext _context;
    private readonly ShortLinkOptions _options;
    private readonly ILogger<ShortLinkService> _logger;

    public ShortLinkService(
        IShortAffiliateLinkRepository shortLinkRepository,
        AppDbContext context,
        IOptions<ShortLinkOptions> options,
        ILogger<ShortLinkService> logger)
    {
        _shortLinkRepository = shortLinkRepository;
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(string PublicShortUrl, Guid AffiliateLinkId)> CreateShortLinkAsync(
        string destinationUrl,
        string originalUrl,
        MarketplaceType platformType,
        int userId,
        Guid tenantId,
        int? integracaoLojaId,
        string? customNickname,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationUrl))
        {
            throw new AffiliateLinkGenerationException("URL de destino do marketplace não informada.");
        }

        var codeLength = _options.ShortCodeLength is >= 6 and <= 8
            ? _options.ShortCodeLength
            : 7;

        string shortCode = string.Empty;
        for (var attempt = 0; attempt < MaxUniqueAttempts; attempt++)
        {
            var candidate = ShortLinkCodeGenerator.Generate(codeLength);
            if (!await _shortLinkRepository.ShortCodeExistsAsync(candidate, cancellationToken))
            {
                shortCode = candidate;
                break;
            }
        }

        if (string.IsNullOrEmpty(shortCode))
        {
            throw new AffiliateLinkGenerationException(
                "Não foi possível gerar um código curto único. Tente novamente.");
        }

        var affiliateUrl = destinationUrl.Trim();
        var (marketplaceAccountId, storeFriendlyName) = await ResolveStoreIdentityAsync(
            tenantId,
            integracaoLojaId,
            platformType,
            cancellationToken);

        var entity = new ShortAffiliateLink
        {
            AffiliateLinkId = Guid.NewGuid(),
            ShortCode = shortCode,
            Code = shortCode,
            DestinationUrl = affiliateUrl,
            AffiliateUrl = affiliateUrl,
            OriginalUrl = originalUrl.Trim(),
            PlatformType = platformType,
            Platform = platformType,
            UserId = userId,
            IntegracaoLojaId = integracaoLojaId,
            MarketplaceAccountId = marketplaceAccountId,
            TenantId = tenantId,
            CustomNickname = customNickname?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.ShortAffiliateLinks.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var publicUrl = ShortLinkPublicUrl.Build(_options.PublicBaseUrl, storeFriendlyName, shortCode);
        _logger.LogInformation(
            "Link encurtado TecFlow criado. Code={ShortCode}, AffiliateLinkId={AffiliateLinkId}, Platform={Platform}",
            shortCode,
            entity.AffiliateLinkId,
            platformType);

        return (publicUrl, entity.AffiliateLinkId);
    }

    private async Task<(int? AccountId, string FriendlyName)> ResolveStoreIdentityAsync(
        Guid tenantId,
        int? integracaoLojaId,
        MarketplaceType platformType,
        CancellationToken cancellationToken)
    {
        string? lojaName = null;
        string? shopId = null;
        if (integracaoLojaId is int lojaId && lojaId > 0)
        {
            var loja = await _context.IntegracaoLojas
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => item.Id == lojaId)
                .Select(item => new { item.ShopId, item.FriendlyName })
                .FirstOrDefaultAsync(cancellationToken);
            shopId = loja?.ShopId;
            lojaName = loja?.FriendlyName;
        }

        int? accountId = null;
        string? accountName = null;
        if (!string.IsNullOrWhiteSpace(shopId))
        {
            var query = _context.MarketplaceAccounts
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(account => account.ShopId == shopId && account.MarketplaceType == platformType);

            if (tenantId != Guid.Empty)
            {
                query = query.Where(account => account.TenantId == tenantId);
            }

            var account = await query
                .Select(item => new { item.Id, item.FriendlyName })
                .FirstOrDefaultAsync(cancellationToken);
            accountId = account?.Id;
            accountName = account?.FriendlyName;
        }

        var friendlyName = !string.IsNullOrWhiteSpace(accountName)
            ? accountName
            : !string.IsNullOrWhiteSpace(lojaName)
                ? lojaName
                : "loja";

        return (accountId, friendlyName);
    }
}

public sealed class LinkClickTelemetryService : ILinkClickTelemetryService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LinkClickTelemetryService> _logger;

    public LinkClickTelemetryService(
        IServiceScopeFactory scopeFactory,
        ILogger<LinkClickTelemetryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RecordGenerationAsync(
        Guid affiliateLinkId,
        Guid tenantId,
        string shopId,
        string originalUrl,
        string convertedUrl,
        MarketplaceType platformType,
        string? ipAddress,
        string? userAgent,
        string? referrerUrl,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILinkClickLogRepository>();
        var log = LinkClickLogFactory.CreateGeneration(
            affiliateLinkId,
            tenantId,
            shopId,
            originalUrl,
            convertedUrl,
            platformType,
            ipAddress,
            userAgent,
            referrerUrl);

        await repository.AddAsync(log, cancellationToken);
        _logger.LogInformation(
            "Telemetria de geração persistida. AffiliateLinkId={AffiliateLinkId} TenantId={TenantId} ShopId={ShopId} Platform={Platform}",
            affiliateLinkId,
            tenantId,
            shopId,
            log.Platform);
    }

    public void EnqueueClickLog(
        Guid affiliateLinkId,
        string? ipAddress,
        string? userAgent,
        string? referrerUrl)
    {
        _ = PersistClickLogAsync(affiliateLinkId, ipAddress, userAgent, referrerUrl);
    }

    private async Task PersistClickLogAsync(
        Guid affiliateLinkId,
        string? ipAddress,
        string? userAgent,
        string? referrerUrl)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<ILinkClickLogRepository>();
            var shortLinkRepository = scope.ServiceProvider.GetRequiredService<IShortAffiliateLinkRepository>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var link = await shortLinkRepository.GetByAffiliateLinkIdAsync(affiliateLinkId);
            if (link is null)
            {
                _logger.LogWarning(
                    "Telemetria de clique ignorada: ShortAffiliateLink {AffiliateLinkId} não encontrado.",
                    affiliateLinkId);
                return;
            }

            var shopId = string.Empty;
            if (link.IntegracaoLojaId is int lojaId)
            {
                shopId = await db.IntegracaoLojas
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(loja => loja.Id == lojaId)
                    .Select(loja => loja.ShopId)
                    .FirstOrDefaultAsync() ?? string.Empty;
            }

            var log = LinkClickLogFactory.CreateClick(
                affiliateLinkId,
                link.TenantId,
                shopId,
                link.OriginalUrl,
                link.DestinationUrl,
                link.PlatformType,
                ipAddress,
                userAgent,
                referrerUrl);

            await repository.AddAsync(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao persistir telemetria de clique para AffiliateLinkId={AffiliateLinkId}.",
                affiliateLinkId);
        }
    }
}

public static class ShortLinkServiceCollectionExtensions
{
    public static IServiceCollection AddTecFlowShortLinkServices(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.Configure<ShortLinkOptions>(configuration.GetSection(ShortLinkOptions.SectionName));
        services.AddScoped<IShortAffiliateLinkRepository, ShortAffiliateLinkRepository>();
        services.AddScoped<ILinkClickLogRepository, LinkClickLogRepository>();
        services.AddScoped<IShortLinkService, ShortLinkService>();
        services.AddScoped<IAffiliateLinkHistoryService, AffiliateLinkHistoryService>();
        services.AddSingleton<ILinkClickTelemetryService, LinkClickTelemetryService>();

        return services;
    }
}

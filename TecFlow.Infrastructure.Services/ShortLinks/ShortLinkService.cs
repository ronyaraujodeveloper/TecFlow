using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Configuration;
using TecFlow.Business.Dto;
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
        var groupId = await ResolveLinkGroupIdAsync(userId, originalUrl, platformType, cancellationToken);
        if (integracaoLojaId is not int lojaId || lojaId <= 0)
        {
            throw new AffiliateLinkGenerationException("Loja não informada para gerar o link encurtado.");
        }

        var created = await EnsureForStoreAsync(
            destinationUrl,
            originalUrl,
            platformType,
            userId,
            tenantId,
            lojaId,
            groupId,
            customNickname,
            cancellationToken);

        return (created.PublicShortUrl, created.AffiliateLinkId);
    }

    public async Task<Guid> ResolveLinkGroupIdAsync(
        int userId,
        string originalUrl,
        MarketplaceType platformType,
        CancellationToken cancellationToken = default)
    {
        var normalizedUrl = originalUrl.Trim();
        var existing = await _context.ShortAffiliateLinks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(link =>
                link.UserId == userId
                && link.OriginalUrl == normalizedUrl
                && link.PlatformType == platformType
                && link.LinkGroupId != Guid.Empty)
            .Select(link => link.LinkGroupId)
            .FirstOrDefaultAsync(cancellationToken);

        return existing == Guid.Empty ? Guid.NewGuid() : existing;
    }

    public async Task<ShortLinkCreateResult> EnsureForStoreAsync(
        string destinationUrl,
        string originalUrl,
        MarketplaceType platformType,
        int userId,
        Guid tenantId,
        int integracaoLojaId,
        Guid linkGroupId,
        string? customNickname,
        CancellationToken cancellationToken = default,
        ProductMetadataDto? productMetadata = null)
    {
        if (string.IsNullOrWhiteSpace(destinationUrl))
        {
            throw new AffiliateLinkGenerationException("URL de destino do marketplace não informada.");
        }

        var affiliateUrl = destinationUrl.Trim();
        var normalizedOriginal = originalUrl.Trim();
        var (marketplaceAccountId, storeFriendlyName) = await ResolveStoreIdentityAsync(
            tenantId,
            integracaoLojaId,
            platformType,
            cancellationToken);

        var existing = await _context.ShortAffiliateLinks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                link =>
                    link.UserId == userId
                    && link.OriginalUrl == normalizedOriginal
                    && link.IntegracaoLojaId == integracaoLojaId
                    && link.PlatformType == platformType,
                cancellationToken);

        ShortAffiliateLink entity;
        if (existing is not null)
        {
            existing.DestinationUrl = affiliateUrl;
            existing.AffiliateUrl = affiliateUrl;
            existing.MarketplaceAccountId = marketplaceAccountId;
            existing.LinkGroupId = linkGroupId == Guid.Empty ? existing.LinkGroupId : linkGroupId;
            existing.CustomNickname = customNickname?.Trim() ?? existing.CustomNickname;
            ApplyProductMetadata(existing, productMetadata);
            existing.IsActive = true;
            existing.Touch();
            entity = existing;
        }
        else
        {
            var shortCode = await GenerateUniqueShortCodeAsync(cancellationToken);
            entity = new ShortAffiliateLink
            {
                AffiliateLinkId = Guid.NewGuid(),
                LinkGroupId = linkGroupId == Guid.Empty ? Guid.NewGuid() : linkGroupId,
                ShortCode = shortCode,
                Code = shortCode,
                DestinationUrl = affiliateUrl,
                AffiliateUrl = affiliateUrl,
                OriginalUrl = normalizedOriginal,
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
            ApplyProductMetadata(entity, productMetadata);

            await _context.ShortAffiliateLinks.AddAsync(entity, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await UpsertAccountAssociationAsync(entity, cancellationToken);

        var publicUrl = ShortLinkPublicUrl.Build(_options.PublicBaseUrl, storeFriendlyName, entity.ShortCode);
        _logger.LogInformation(
            "Link encurtado TecFlow persistido. Code={ShortCode}, AffiliateLinkId={AffiliateLinkId}, Loja={LojaId}, Ativo={IsActive}",
            entity.ShortCode,
            entity.AffiliateLinkId,
            integracaoLojaId,
            entity.IsActive);

        return new ShortLinkCreateResult
        {
            PublicShortUrl = publicUrl,
            AffiliateLinkId = entity.AffiliateLinkId,
            ShortCode = entity.ShortCode
        };
    }

    public async Task DeactivateUnselectedAccountsAsync(
        Guid linkGroupId,
        IReadOnlyCollection<int> selectedIntegracaoLojaIds,
        CancellationToken cancellationToken = default)
    {
        if (linkGroupId == Guid.Empty)
        {
            return;
        }

        var selected = selectedIntegracaoLojaIds.ToHashSet();
        var links = await _context.ShortAffiliateLinks
            .IgnoreQueryFilters()
            .Where(link => link.LinkGroupId == linkGroupId)
            .ToListAsync(cancellationToken);

        var associations = await _context.ShortAffiliateLinkAccounts
            .IgnoreQueryFilters()
            .Where(account => account.LinkGroupId == linkGroupId)
            .ToListAsync(cancellationToken);

        foreach (var link in links)
        {
            var lojaId = link.IntegracaoLojaId ?? 0;
            if (lojaId > 0 && selected.Contains(lojaId))
            {
                continue;
            }

            if (!link.IsActive)
            {
                continue;
            }

            link.IsActive = false;
            link.Touch();
        }

        foreach (var association in associations)
        {
            if (selected.Contains(association.IntegracaoLojaId))
            {
                continue;
            }

            if (!association.IsActive)
            {
                continue;
            }

            association.IsActive = false;
            association.Touch();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyProductMetadata(ShortAffiliateLink entity, ProductMetadataDto? metadata)
    {
        if (metadata is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(metadata.ProductName))
        {
            var name = metadata.ProductName.Trim();
            entity.ProductName = name.Length <= 255 ? name : name[..255];
        }

        entity.ProductPrice = metadata.ProductPrice;

        if (!string.IsNullOrWhiteSpace(metadata.ProductImageUrl))
        {
            var image = metadata.ProductImageUrl.Trim();
            entity.ProductImageUrl = image.Length <= 500 ? image : image[..500];
        }
    }

    private async Task UpsertAccountAssociationAsync(
        ShortAffiliateLink entity,
        CancellationToken cancellationToken)
    {
        var lojaId = entity.IntegracaoLojaId ?? 0;
        if (lojaId <= 0)
        {
            return;
        }

        var association = await _context.ShortAffiliateLinkAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                account => account.LinkGroupId == entity.LinkGroupId && account.IntegracaoLojaId == lojaId,
                cancellationToken);

        if (association is null)
        {
            association = new ShortAffiliateLinkAccount
            {
                TenantId = entity.TenantId,
                LinkGroupId = entity.LinkGroupId,
                ShortAffiliateLinkId = entity.Id,
                IntegracaoLojaId = lojaId,
                MarketplaceAccountId = entity.MarketplaceAccountId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ShortAffiliateLinkAccounts.AddAsync(association, cancellationToken);
        }
        else
        {
            association.ShortAffiliateLinkId = entity.Id;
            association.MarketplaceAccountId = entity.MarketplaceAccountId;
            association.IsActive = true;
            association.Touch();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateUniqueShortCodeAsync(CancellationToken cancellationToken)
    {
        var codeLength = _options.ShortCodeLength is >= 6 and <= 8
            ? _options.ShortCodeLength
            : 7;

        for (var attempt = 0; attempt < MaxUniqueAttempts; attempt++)
        {
            var candidate = ShortLinkCodeGenerator.Generate(codeLength);
            if (!await _shortLinkRepository.ShortCodeExistsAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new AffiliateLinkGenerationException(
            "Não foi possível gerar um código curto único. Tente novamente.");
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

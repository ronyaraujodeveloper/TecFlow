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

namespace TecFlow.Infrastructure.Services.ShortLinks;

public sealed class ShortLinkService : IShortLinkService
{
    private const int MaxUniqueAttempts = 8;

    private readonly IShortAffiliateLinkRepository _shortLinkRepository;
    private readonly ShortLinkOptions _options;
    private readonly ILogger<ShortLinkService> _logger;

    public ShortLinkService(
        IShortAffiliateLinkRepository shortLinkRepository,
        IOptions<ShortLinkOptions> options,
        ILogger<ShortLinkService> logger)
    {
        _shortLinkRepository = shortLinkRepository;
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

        var entity = new ShortAffiliateLink
        {
            AffiliateLinkId = Guid.NewGuid(),
            ShortCode = shortCode,
            DestinationUrl = destinationUrl.Trim(),
            OriginalUrl = originalUrl.Trim(),
            PlatformType = platformType,
            UserId = userId,
            IntegracaoLojaId = integracaoLojaId,
            TenantId = tenantId,
            CustomNickname = customNickname?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _shortLinkRepository.AddAsync(entity, cancellationToken);

        var publicUrl = BuildPublicUrl(shortCode);
        _logger.LogInformation(
            "Link encurtado TecFlow criado. Code={ShortCode}, AffiliateLinkId={AffiliateLinkId}, Platform={Platform}",
            shortCode,
            entity.AffiliateLinkId,
            platformType);

        return (publicUrl, entity.AffiliateLinkId);
    }

    private string BuildPublicUrl(string shortCode)
    {
        var baseUrl = (_options.PublicBaseUrl ?? "http://localhost:5001/r").TrimEnd('/');
        return $"{baseUrl}/{shortCode}";
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

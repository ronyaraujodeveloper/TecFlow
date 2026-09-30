using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Application;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Business.Service.PublicPages;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Infrastructure.Services.LinkStrategies;

namespace TecFlow.Infrastructure.Services.PublicPages;

public sealed class PublicConverterPageService : IPublicConverterPageService
{
    private readonly AppDbContext _context;
    private readonly IMarketplaceAccountRepository _marketplaceAccounts;
    private readonly PlatformLinkResolver _platformLinkResolver;
    private readonly IAffiliateLinkGenerationService _generationService;
    private readonly ILogger<PublicConverterPageService> _logger;

    public PublicConverterPageService(
        AppDbContext context,
        IMarketplaceAccountRepository marketplaceAccounts,
        PlatformLinkResolver platformLinkResolver,
        IAffiliateLinkGenerationService generationService,
        ILogger<PublicConverterPageService> logger)
    {
        _context = context;
        _marketplaceAccounts = marketplaceAccounts;
        _platformLinkResolver = platformLinkResolver;
        _generationService = generationService;
        _logger = logger;
    }

    public async Task<PublicConverterPageDto?> ResolveBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var page = await FindBySlugAsync(slug, cancellationToken);
        if (page is null)
        {
            return null;
        }

        var platforms = await LoadDistinctPlatformsAsync(page.UserId, cancellationToken);
        return Map(page, platforms);
    }

    public async Task<PublicConverterPageResponseDto> ListMineAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActivePageAsync(userId, cancellationToken);
        var pages = await _context.PublicConverterPages
            .IgnoreQueryFilters()
            .Where(page => page.UserId == userId)
            .OrderByDescending(page => page.IsActive)
            .ThenByDescending(page => page.CreatedAt)
            .ToListAsync(cancellationToken);

        return new PublicConverterPageResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = pages.Select(page => Map(page)).ToList(),
            Data = pages.FirstOrDefault(page => page.IsActive) is { } active ? Map(active) : pages.Select(page => Map(page)).FirstOrDefault()
        };
    }

    public async Task<PublicConverterPageResponseDto> ChangeSlugAsync(
        int userId,
        string newSlug,
        CancellationToken cancellationToken = default)
    {
        if (!PublicConverterRules.IsValidSlug(newSlug))
        {
            return Fail("Informe um slug com 3 a 64 caracteres (letras, números ou hífen).");
        }

        var normalized = PublicConverterRules.NormalizeSlug(newSlug);
        var active = await EnsureActivePageAsync(userId, cancellationToken);
        if (string.Equals(active.Slug, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return new PublicConverterPageResponseDto
            {
                Status = true,
                Descricao = "Slug inalterado.",
                Data = Map(active)
            };
        }

        var taken = await _context.PublicConverterPages
            .IgnoreQueryFilters()
            .AnyAsync(
                page => page.Slug == normalized && page.PublicCode != active.PublicCode,
                cancellationToken);
        if (taken)
        {
            return Fail("Este slug já está em uso. Escolha outro endereço público.");
        }

        var created = PublicConverterRules.CreateVersionedSlug(active, normalized);
        await _context.PublicConverterPages.AddAsync(created, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Slug da página pública versionado. UserId={UserId} PublicCode={PublicCode} De={OldSlug} Para={NewSlug}",
            userId,
            created.PublicCode,
            active.Slug,
            created.Slug);

        return new PublicConverterPageResponseDto
        {
            Status = true,
            Descricao = "Slug atualizado. O endereço anterior continua convertendo.",
            Data = Map(created)
        };
    }

    public async Task<GerarLinkAfiliadoResponseDto> ConvertAsync(
        string slug,
        string originalUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return new GerarLinkAfiliadoResponseDto
            {
                Success = false,
                Status = false,
                Message = "Informe a URL do produto para gerar o link de afiliado.",
                Descricao = "Informe a URL do produto para gerar o link de afiliado."
            };
        }

        var page = await FindBySlugAsync(slug, cancellationToken);
        if (page is null)
        {
            return new GerarLinkAfiliadoResponseDto
            {
                Success = false,
                Status = false,
                Message = "Página pública não encontrada.",
                Descricao = "Página pública não encontrada."
            };
        }

        var workingUrl = originalUrl.Trim();
        string resolvedUrl;
        try
        {
            (_, resolvedUrl) = await _platformLinkResolver.ResolveFromInputAsync(workingUrl, cancellationToken);
        }
        catch (AffiliateLinkGenerationException ex)
        {
            return new GerarLinkAfiliadoResponseDto
            {
                Success = false,
                Status = false,
                Message = ex.Message,
                Descricao = ex.Message
            };
        }

        if (!UrlUnshortenerService.TryDetectMarketplace(resolvedUrl, out var platform)
            && !UniversalLinkResolverEngine.TryMapDomainToPlatform(resolvedUrl, out platform))
        {
            return new GerarLinkAfiliadoResponseDto
            {
                Success = false,
                Status = false,
                Message = ShortAffiliateLinkService.UnrecognizedDestinationMessage,
                Descricao = ShortAffiliateLinkService.UnrecognizedDestinationMessage
            };
        }

        var accounts = await _marketplaceAccounts.ListByUserIdAsync(
            page.UserId.ToString(),
            cancellationToken);
        var account = PublicConverterRules.FirstActiveForPlatform(accounts, platform);
        if (account is null)
        {
            return new GerarLinkAfiliadoResponseDto
            {
                Success = false,
                Status = false,
                Message = IntegracaoLojaScopeResolver.MissingConnectedAccountMessage(platform),
                Descricao = IntegracaoLojaScopeResolver.MissingConnectedAccountMessage(platform)
            };
        }

        var request = new GerarLinkAfiliadoDto
        {
            OriginalUrl = originalUrl.Trim(),
            StoreId = IntegracaoLojaScopeHelper.EncodeStoreScope(account.Id),
            StoreIds = [IntegracaoLojaScopeHelper.EncodeStoreScope(account.Id)],
            TenantId = account.TenantId,
            ShopId = account.ShopId,
            Source = PublicConverterRules.PublicPageSource
        };

        var result = await _generationService.GenerateAsync(request, page.UserId, cancellationToken);
        result.IsFromPublicPage = true;
        return result;
    }

    private async Task<PublicConverterPage?> FindBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var normalized = PublicConverterRules.NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return await _context.PublicConverterPages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                page => page.Slug == normalized,
                cancellationToken);
    }

    private async Task<PublicConverterPage> EnsureActivePageAsync(int userId, CancellationToken cancellationToken)
    {
        var pages = await _context.PublicConverterPages
            .IgnoreQueryFilters()
            .Where(page => page.UserId == userId)
            .ToListAsync(cancellationToken);
        var active = pages.FirstOrDefault(page => page.IsActive);
        if (active is not null)
        {
            return active;
        }

        var user = await _context.UserAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        var tenantId = user?.TenantId ?? Guid.Empty;
        var created = new PublicConverterPage
        {
            PublicCode = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            DisplayName = user?.Name,
            Slug = PublicConverterRules.NormalizeSlug($"afiliado-{userId}"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.PublicConverterPages.AddAsync(created, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<List<MarketplaceType>> LoadDistinctPlatformsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var accounts = await _marketplaceAccounts.ListByUserIdAsync(userId.ToString(), cancellationToken);
        return PublicConverterRules.DistinctActivePlatforms(accounts)
            .Select(account => account.MarketplaceType)
            .ToList();
    }

    private static PublicConverterPageDto Map(
        PublicConverterPage page,
        IReadOnlyList<MarketplaceType>? platforms = null) =>
        new()
        {
            Id = page.Id,
            PublicCode = page.PublicCode,
            Slug = page.Slug,
            DisplayName = page.DisplayName,
            IsActive = page.IsActive,
            CreatedAt = page.CreatedAt,
            ConnectedPlatforms = platforms?.ToList() ?? []
        };

    private static PublicConverterPageResponseDto Fail(string message) =>
        new()
        {
            Status = false,
            Descricao = message
        };
}

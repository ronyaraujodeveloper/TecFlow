using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Repositories;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Application;
using Ofertio.Business.Service.LinkStrategies;
using Ofertio.Business.Service.Master;
using Ofertio.Business.Service.PublicPages;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;
using Ofertio.Infrastructure.Services.LinkStrategies;

namespace Ofertio.Infrastructure.Services.PublicPages;

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
        try
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
                CanCustomizeSlug = await CanCustomizeSlugAsync(userId, cancellationToken),
                DataList = pages.Select(page => Map(page)).ToList(),
                Data = pages.FirstOrDefault(page => page.IsActive) is { } active ? Map(active) : pages.Select(page => Map(page)).FirstOrDefault()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao listar páginas públicas. UserId={UserId}", userId);
            return Fail("Não foi possível carregar as páginas públicas. Nenhuma página cadastrada ou o banco ainda está vazio.");
        }
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
                CanCustomizeSlug = await CanCustomizeSlugAsync(userId, cancellationToken),
                Data = Map(active)
            };
        }

        if (!await CanCustomizeSlugAsync(userId, cancellationToken))
        {
            return Fail("Seu plano não libera a personalização do slug.");
        }

        var taken = await _context.PublicConverterPages
            .IgnoreQueryFilters()
            .AnyAsync(page => page.Slug == normalized && page.Id != active.Id, cancellationToken);
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
            CanCustomizeSlug = true,
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
            Slug = await CreateUniqueSlugAsync(cancellationToken),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.PublicConverterPages.AddAsync(created, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<string> CreateUniqueSlugAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var candidate = PublicConverterRules.CreateRandomSlug();
            var taken = await _context.PublicConverterPages
                .IgnoreQueryFilters()
                .AnyAsync(page => page.Slug == candidate, cancellationToken);
            if (!taken)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Não foi possível gerar um endereço público único.");
    }

    /// <summary>
    /// Linha "Slug personalizado" inativa bloqueia todos. Ativa: contas Master liberadas (como no menu do
    /// Painel Master, que ignora as colunas de plano); clientes precisam da coluna do próprio plano marcada.
    /// </summary>
    private async Task<bool> CanCustomizeSlugAsync(int userId, CancellationToken cancellationToken)
    {
        var menuItemId = await _context.MenuItems
            .AsNoTracking()
            .Where(item => item.Key == DefaultMenuCatalog.CustomPublicSlug && item.IsActive)
            .Select(item => (int?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (menuItemId is null)
        {
            return false;
        }

        var user = await _context.UserAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new { item.TenantId, item.Plan, item.Role })
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            return false;
        }

        if (MasterRoles.IsMaster(user.Role))
        {
            return true;
        }

        var tenantPlan = await _context.Tenants
            .AsNoTracking()
            .Where(item => item.Id == user.TenantId)
            .Select(item => item.PlanCode)
            .FirstOrDefaultAsync(cancellationToken);
        var code = (string.IsNullOrWhiteSpace(tenantPlan) ? user.Plan : tenantPlan)?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var planId = await _context.SaasPlans
            .AsNoTracking()
            .Where(plan => plan.Code == code && plan.IsActive)
            .Select(plan => (int?)plan.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (planId is null)
        {
            return false;
        }

        return await _context.PlanMenuPermissions
            .AsNoTracking()
            .AnyAsync(
                permission => permission.PlanId == planId && permission.MenuItemId == menuItemId && permission.IsVisible,
                cancellationToken);
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

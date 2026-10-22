using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Groups;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Enums;
using Ofertio.Database;
using Ofertio.Database.Filter;

namespace Ofertio.Infrastructure.Services.Radar;

public sealed class OfficialCatalogSearchService : IOfficialCatalogSearchService
{
    private readonly IMercadoLivreApiService _mercadoLivre;
    private readonly IShopeeAffiliateOfferService _shopee;
    private readonly IAmazonPaApiService _amazon;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<OfficialCatalogSearchService> _logger;

    public OfficialCatalogSearchService(
        IMercadoLivreApiService mercadoLivre,
        IShopeeAffiliateOfferService shopee,
        IAmazonPaApiService amazon,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ILogger<OfficialCatalogSearchService> logger)
    {
        _mercadoLivre = mercadoLivre;
        _shopee = shopee;
        _amazon = amazon;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<OfficialCatalogSearchResponseDto> SearchAsync(
        int userId,
        OfficialCatalogSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        var keyword = filter.Keyword?.Trim() ?? string.Empty;
        if (!OfficialCatalogSearchRules.IsValidQuery(keyword))
        {
            return new OfficialCatalogSearchResponseDto
            {
                Status = false,
                Descricao = "Informe ao menos 2 caracteres para buscar nas lojas oficiais.",
                DataList = [],
                Channels = []
            };
        }

        var includeMl = filter.MercadoLivre;
        var includeShopee = filter.Shopee;
        var includeAmazon = filter.Amazon;
        if (!includeMl && !includeShopee && !includeAmazon)
        {
            includeMl = includeShopee = includeAmazon = true;
        }

        var limit = OfficialCatalogSearchRules.ClampLimit(filter.Limit);
        var skipped = Task.FromResult(OfficialCatalogChannelResult.Empty);
        var mlTask = includeMl
            ? SearchMercadoLivreSafeAsync(userId, keyword, limit, filter, cancellationToken)
            : skipped;
        var shopeeTask = includeShopee
            ? _shopee.SearchProductsAsync(userId, keyword, limit, cancellationToken)
            : skipped;
        var amazonTask = includeAmazon
            ? _amazon.SearchProductsAsync(userId, keyword, limit, cancellationToken)
            : skipped;
        var internalTask = filter.MercadoLivreOffset > 0
            ? Task.FromResult<IReadOnlyList<OfficialCatalogProductDto>>([])
            : SearchLocalProductsAsync(keyword, limit, cancellationToken);
        var ratesTask = LoadCommissionRatesAsync(userId, cancellationToken);
        await Task.WhenAll(mlTask, shopeeTask, amazonTask, internalTask, ratesTask);
        var (mercadoLivreRate, amazonRate) = await ratesTask;

        var ml = await mlTask;
        var shopee = await shopeeTask;
        var amazon = await amazonTask;
        var mlItems = ml.Items;
        var shopeeItems = shopee.Items;
        var amazonItems = amazon.Items;
        var localItems = await internalTask;

        _logger.LogInformation(
            "Busca concluída para '{query}': ML={mlCount}, Shopee={shopeeCount}, Amazon={amazonCount}, Local={localCount}",
            keyword,
            mlItems.Count,
            shopeeItems.Count,
            amazonItems.Count,
            localItems.Count);

        var deduplicated = mlItems
            .Concat(shopeeItems)
            .Concat(amazonItems)
            .Concat(localItems)
            .Where(item => OfficialCatalogSearchRules.PassesOptionalFilters(item, filter))
            .GroupBy(item => item.SourceUrl.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        foreach (var item in deduplicated)
        {
            OfficialCatalogSearchRules.ApplyCommission(item, mercadoLivreRate, amazonRate);
            OfficialCatalogSearchRules.ScoreOffer(item);
        }

        var merged = OfficialCatalogSearchRules.ApplySort(deduplicated, filter.SortBy).ToList();

        var mlStatus = OfficialCatalogSearchRules.BuildChannelStatus(
            "MercadoLivre",
            "Mercado Livre",
            includeMl,
            OfficialCatalogSearchRules.CountVisibleOffers(mlItems, filter),
            ml.MissingCredentials,
            OfficialCatalogSearchRules.MercadoLivreMissingAccountMessage,
            ml.MissingCredentials ? null : ml.ErrorMessage);
        if (includeMl && ml.ScannedCount > 0 && mlStatus.State is "ok" or "empty")
        {
            mlStatus.Message = OfficialCatalogSearchRules.DescribeMercadoLivreScan(
                mlStatus.ProductCount,
                ml.ScannedCount,
                ml.EffectiveQuery,
                keyword);
        }

        return new OfficialCatalogSearchResponseDto
        {
            Status = true,
            Descricao = merged.Count == 0
                ? "Nenhuma oferta encontrada nas APIs oficiais para este termo."
                : $"{merged.Count} oferta(s) encontradas.",
            DataList = merged,
            MercadoLivreNextOffset = includeMl ? ml.NextOffset : null,
            MercadoLivreEffectiveQuery = includeMl ? ml.EffectiveQuery : null,
            MercadoLivreCategories = includeMl ? ml.Categories.ToList() : [],
            Channels =
            [
                mlStatus,
                OfficialCatalogSearchRules.BuildChannelStatus(
                    "Shopee",
                    "Shopee",
                    includeShopee,
                    shopeeItems.Count,
                    shopee.MissingCredentials,
                    OfficialCatalogSearchRules.ShopeeMissingApiKeyMessage),
                OfficialCatalogSearchRules.BuildChannelStatus(
                    "Amazon",
                    "Amazon",
                    includeAmazon,
                    amazonItems.Count,
                    amazon.MissingCredentials,
                    OfficialCatalogSearchRules.AmazonMissingPaApiMessage),
                OfficialCatalogSearchRules.BuildChannelStatus(
                    "Local",
                    "Base Local (Grupos)",
                    true,
                    localItems.Count,
                    false,
                    string.Empty)
            ]
        };
    }

    private async Task<OfficialCatalogChannelResult> SearchMercadoLivreSafeAsync(
        int userId,
        string keyword,
        int limit,
        OfficialCatalogSearchFilter filter,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _mercadoLivre.SearchCatalogAsync(
                userId,
                new MercadoLivreCatalogSearchRequest
                {
                    Query = keyword,
                    TargetOffers = limit,
                    Offset = Math.Max(0, filter.MercadoLivreOffset),
                    DomainId = string.IsNullOrWhiteSpace(filter.MercadoLivreCategory) ? null : filter.MercadoLivreCategory.Trim(),
                    MinPrice = filter.MinPrice,
                    MaxPrice = filter.MaxPrice,
                    FreeShippingOnly = filter.FreeShippingOnly == true,
                    MainProductsOnly = filter.MainProductsOnly == true,
                    AllowShorterQuery = filter.MercadoLivreOffset <= 0,
                    IncludeCategories = filter.MercadoLivreOffset <= 0,
                    TrackingId = filter.MercadoLivreTrackingId
                },
                cancellationToken);
        }
        catch (JsonException)
        {
            return OfficialCatalogChannelResult.Failed("Erro de Desserialização");
        }
        catch (Exception ex) when (OfficialCatalogSearchRules.IsHttpTimeout(ex, cancellationToken))
        {
            _logger.LogWarning("[ML Busca] A busca '{Query}' excedeu o tempo limite da API do Mercado Livre.", keyword);
            return OfficialCatalogChannelResult.Failed(OfficialCatalogSearchRules.MercadoLivreTimeoutMessage);
        }
        catch (Exception ex)
        {
            return OfficialCatalogChannelResult.Failed(
                string.IsNullOrWhiteSpace(ex.Message) ? "Erro inesperado" : ex.Message);
        }
    }

    private async Task<(decimal? MercadoLivre, decimal? Amazon)> LoadCommissionRatesAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var profile = await dbContext.AffiliateMiningProfiles
                .AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => new { item.MercadoLivreCommissionPercent, item.AmazonCommissionPercent })
                .FirstOrDefaultAsync(cancellationToken);
            return (profile?.MercadoLivreCommissionPercent, profile?.AmazonCommissionPercent);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Não foi possível carregar as taxas de comissão do usuário {UserId}.", userId);
            return (null, null);
        }
    }

    private async Task<IReadOnlyList<OfficialCatalogProductDto>> SearchLocalProductsAsync(
        string keyword,
        int limit,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await dbContext.GroupCapturedMessages
            .AsNoTracking()
            .Where(item => item.Type == OfferType.Product
                && ((item.ProductName != null && item.ProductName.Contains(keyword))
                    || item.OriginalUrl.Contains(keyword)))
            .OrderByCapturedAtDescending()
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(item => new OfficialCatalogProductDto
            {
                Platform = item.PlatformName ?? item.PlatformType?.ToString() ?? "Interno",
                PlatformType = item.PlatformType,
                ProductName = item.ProductName,
                Price = item.ValidatedPrice ?? item.ExtractedPrice,
                CouponCode = item.CouponCode,
                ImageUrl = item.ProductImageUrl,
                SourceUrl = string.IsNullOrWhiteSpace(item.PrimaryProductUrl) ? item.OriginalUrl : item.PrimaryProductUrl,
                Source = "Interno"
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.SourceUrl))
            .ToList();
    }
}

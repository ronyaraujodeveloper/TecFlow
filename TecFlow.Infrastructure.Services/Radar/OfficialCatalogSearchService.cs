using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Database.Filter;

namespace TecFlow.Infrastructure.Services.Radar;

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
            ? SearchMercadoLivreSafeAsync(keyword, limit, cancellationToken)
            : skipped;
        var shopeeTask = includeShopee
            ? _shopee.SearchProductsAsync(userId, keyword, limit, cancellationToken)
            : skipped;
        var amazonTask = includeAmazon
            ? _amazon.SearchProductsAsync(userId, keyword, limit, cancellationToken)
            : skipped;
        var internalTask = SearchLocalProductsAsync(keyword, limit, cancellationToken);
        await Task.WhenAll(mlTask, shopeeTask, amazonTask, internalTask);

        var mlItems = mlTask.Result.Items;
        var shopeeItems = shopeeTask.Result.Items;
        var amazonItems = amazonTask.Result.Items;
        var localItems = internalTask.Result;

        _logger.LogInformation(
            "Busca concluída para '{query}': ML={mlCount}, Shopee={shopeeCount}, Amazon={amazonCount}, Local={localCount}",
            keyword,
            mlItems.Count,
            shopeeItems.Count,
            amazonItems.Count,
            localItems.Count);

        var merged = mlItems
            .Concat(shopeeItems)
            .Concat(amazonItems)
            .Concat(localItems)
            .Where(item => LiveSearchRules.Matches(
                item.ProductName,
                item.Price,
                item.CouponCode,
                item.Platform,
                item.PlatformType,
                keyword,
                filter.MinPrice,
                filter.MaxPrice,
                filter.HasCoupon,
                store: null))
            .GroupBy(item => item.SourceUrl.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        return new OfficialCatalogSearchResponseDto
        {
            Status = true,
            Descricao = merged.Count == 0
                ? "Nenhuma oferta encontrada nas APIs oficiais para este termo."
                : $"{merged.Count} oferta(s) encontradas.",
            DataList = merged,
            Channels =
            [
                OfficialCatalogSearchRules.BuildChannelStatus(
                    "MercadoLivre",
                    "Mercado Livre",
                    includeMl,
                    mlItems.Count,
                    false,
                    string.Empty,
                    mlTask.Result.ErrorMessage),
                OfficialCatalogSearchRules.BuildChannelStatus(
                    "Shopee",
                    "Shopee",
                    includeShopee,
                    shopeeItems.Count,
                    shopeeTask.Result.MissingCredentials,
                    OfficialCatalogSearchRules.ShopeeMissingApiKeyMessage),
                OfficialCatalogSearchRules.BuildChannelStatus(
                    "Amazon",
                    "Amazon",
                    includeAmazon,
                    amazonItems.Count,
                    amazonTask.Result.MissingCredentials,
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
        string keyword,
        int limit,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _mercadoLivre.SearchProductsAsync(keyword, limit, cancellationToken);
        }
        catch (JsonException)
        {
            return OfficialCatalogChannelResult.Failed("Erro de Desserialização");
        }
        catch (Exception ex)
        {
            return OfficialCatalogChannelResult.Failed(
                string.IsNullOrWhiteSpace(ex.Message) ? "Erro inesperado" : ex.Message);
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
            .Where(item =>
                (item.ProductName != null && item.ProductName.Contains(keyword))
                || item.OriginalUrl.Contains(keyword))
            .OrderByDescending(item => item.CreatedAt)
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

using Microsoft.EntityFrameworkCore;
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
    private readonly AppDbContext _context;

    public OfficialCatalogSearchService(
        IMercadoLivreApiService mercadoLivre,
        IShopeeAffiliateOfferService shopee,
        IAmazonPaApiService amazon,
        AppDbContext context)
    {
        _mercadoLivre = mercadoLivre;
        _shopee = shopee;
        _amazon = amazon;
        _context = context;
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
                DataList = []
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
        var mlTask = includeMl
            ? _mercadoLivre.SearchProductsAsync(keyword, limit, cancellationToken)
            : Task.FromResult<IReadOnlyList<OfficialCatalogProductDto>>([]);
        var shopeeTask = includeShopee
            ? _shopee.SearchProductsAsync(userId, keyword, limit, cancellationToken)
            : Task.FromResult<IReadOnlyList<OfficialCatalogProductDto>>([]);
        var amazonTask = includeAmazon
            ? _amazon.SearchProductsAsync(userId, keyword, limit, cancellationToken)
            : Task.FromResult<IReadOnlyList<OfficialCatalogProductDto>>([]);
        var internalTask = SearchInternalAsync(keyword, limit, cancellationToken);
        await Task.WhenAll(mlTask, shopeeTask, amazonTask, internalTask);

        var merged = mlTask.Result
            .Concat(shopeeTask.Result)
            .Concat(amazonTask.Result)
            .Concat(internalTask.Result)
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
            DataList = merged
        };
    }

    private async Task<IReadOnlyList<OfficialCatalogProductDto>> SearchInternalAsync(
        string keyword,
        int limit,
        CancellationToken cancellationToken)
    {
        var rows = await _context.GroupCapturedMessages
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

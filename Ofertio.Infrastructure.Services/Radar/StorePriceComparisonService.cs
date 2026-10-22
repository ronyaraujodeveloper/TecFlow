using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Radar;
using Ofertio.Database;
using Ofertio.Database.Filter;

namespace Ofertio.Infrastructure.Services.Radar;

public sealed class StorePriceComparisonService : IStorePriceComparisonService
{
    private readonly AppDbContext _context;
    private readonly IOfficialCatalogSearchService _catalog;
    private readonly ILogger<StorePriceComparisonService> _logger;

    public StorePriceComparisonService(
        AppDbContext context,
        IOfficialCatalogSearchService catalog,
        ILogger<StorePriceComparisonService> logger)
    {
        _context = context;
        _catalog = catalog;
        _logger = logger;
    }

    public async Task<StorePriceComparisonResponseDto> CompareAsync(
        int userId,
        StorePriceComparisonRequestDto request,
        CancellationToken cancellationToken = default)
    {
        request ??= new StorePriceComparisonRequestDto();
        var model = ProductModelRules.ExtractProductModel(request.ProductName);
        var exact = ProductModelRules.FindModelCode(request.ProductName).Length > 0;
        var query = model.Length >= OfficialCatalogSearchRules.MinKeywordLength
            ? model
            : request.ProductName?.Trim() ?? string.Empty;
        var found = new List<StorePriceComparisonRowDto>();

        if (OfficialCatalogSearchRules.IsValidQuery(query))
        {
            try
            {
                var search = await _catalog.SearchAsync(
                    userId,
                    new OfficialCatalogSearchFilter
                    {
                        Keyword = query,
                        Limit = OfficialCatalogSearchRules.MaxLimit
                    },
                    cancellationToken);
                found.AddRange((search.DataList ?? []).Select(MapCatalog));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao comparar o modelo '{Model}' nas APIs oficiais. UserId={UserId}", query, userId);
            }
        }

        found.AddRange(await LoadLocalAsync(userId, cancellationToken));
        var rows = OfferIndexRules.BuildComparisonTable(MapRequest(request), found, OfferIndexRules.MaxComparisonRows);
        var others = rows.Count(row => !row.IsReference);
        var description = others == 0
            ? exact
                ? "Nenhuma outra loja encontrou este modelo agora."
                : "Não foi possível isolar o código do modelo e nenhuma oferta parecida foi encontrada."
            : exact
                ? $"Comparação do modelo {model} em {rows.Count} loja(s)."
                : "Não foi possível isolar o código do modelo. Mostrando ofertas parecidas.";

        return new StorePriceComparisonResponseDto
        {
            Status = true,
            Descricao = description,
            Model = model,
            ExactModel = exact,
            Rows = rows
        };
    }

    private async Task<IReadOnlyList<StorePriceComparisonRowDto>> LoadLocalAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.Subtract(AffiliateMiningRules.HistoryWindow);
        var rows = await _context.IndexedCatalogOffers
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.IndexedAt >= since)
            .OrderByDescending(item => item.IndexedAt)
            .Take(300)
            .ToListAsync(cancellationToken);
        return rows.Select(item => new StorePriceComparisonRowDto
        {
            ProductName = item.ProductName,
            ImageUrl = item.ProductImageUrl,
            SourceUrl = item.SourceUrl,
            PlatformType = item.PlatformType,
            PlatformName = item.PlatformName,
            Price = item.Price,
            Shipping = item.Shipping,
            CommissionPercent = item.CommissionPercent,
            EstimatedCommission = item.EstimatedCommission
        }).ToList();
    }

    private static StorePriceComparisonRowDto MapRequest(StorePriceComparisonRequestDto request) =>
        new()
        {
            ProductName = string.IsNullOrWhiteSpace(request.ProductName) ? "Produto selecionado" : request.ProductName.Trim(),
            ImageUrl = request.ImageUrl,
            SourceUrl = request.SourceUrl?.Trim() ?? string.Empty,
            PlatformType = request.PlatformType,
            PlatformName = request.PlatformName,
            Price = OfferIndexRules.RoundMoney(request.Price),
            Shipping = request.Shipping,
            CommissionPercent = request.CommissionPercent,
            EstimatedCommission = request.EstimatedCommission,
            IsReference = true
        };

    private static StorePriceComparisonRowDto MapCatalog(OfficialCatalogProductDto item) =>
        new()
        {
            ProductName = string.IsNullOrWhiteSpace(item.ProductName) ? "Oferta" : item.ProductName.Trim(),
            ImageUrl = item.ImageUrl,
            SourceUrl = item.SourceUrl?.Trim() ?? string.Empty,
            PlatformType = item.PlatformType,
            PlatformName = string.IsNullOrWhiteSpace(item.Platform) ? item.PlatformType?.ToString() : item.Platform,
            Price = OfferIndexRules.RoundMoney(item.Price),
            Shipping = item.Shipping,
            CommissionPercent = item.CommissionPercent,
            EstimatedCommission = item.EstimatedCommission
        };
}

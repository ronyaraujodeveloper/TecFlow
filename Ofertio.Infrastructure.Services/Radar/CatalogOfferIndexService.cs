using Microsoft.EntityFrameworkCore;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Entities;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Radar;

public sealed class CatalogOfferIndexService : ICatalogOfferIndex
{
    private readonly AppDbContext _context;

    public CatalogOfferIndexService(AppDbContext context)
    {
        _context = context;
    }

    public async Task UpsertAsync(
        int userId,
        string searchTerm,
        IReadOnlyCollection<OfficialCatalogProductDto> offers,
        CancellationToken cancellationToken = default)
    {
        var term = OfferIndexRules.NormalizeTerm(searchTerm);
        if (userId <= 0 || term.Length < 2 || offers.Count == 0)
        {
            return;
        }

        var incoming = offers
            .Select(offer => (Url: OfferIndexRules.Clip(offer.SourceUrl, 1000), Offer: offer))
            .Where(item => item.Url.Length > 0)
            .GroupBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        if (incoming.Count == 0)
        {
            return;
        }

        var urls = incoming.Select(item => item.Url).ToList();
        var existing = await _context.IndexedCatalogOffers
            .Where(item => item.UserId == userId && urls.Contains(item.SourceUrl))
            .ToListAsync(cancellationToken);
        var byUrl = existing.ToDictionary(item => item.SourceUrl, StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;

        foreach (var (url, offer) in incoming)
        {
            var name = OfferIndexRules.Clip(
                string.IsNullOrWhiteSpace(offer.ProductName) ? "Oferta indexada" : offer.ProductName,
                255);
            var model = OfferIndexRules.Clip(ProductModelRules.ExtractProductModel(offer.ProductName), 80);
            if (!byUrl.TryGetValue(url, out var row))
            {
                row = new IndexedCatalogOffer
                {
                    UserId = userId,
                    SourceUrl = url
                };
                _context.IndexedCatalogOffers.Add(row);
                byUrl[url] = row;
            }

            row.SearchTerm = OfferIndexRules.Clip(term, 120);
            row.ProductName = name;
            row.ProductModel = string.IsNullOrWhiteSpace(model) ? null : model;
            row.ProductImageUrl = NullIfEmpty(OfferIndexRules.Clip(offer.ImageUrl, 500));
            row.PlatformType = offer.PlatformType;
            row.PlatformName = NullIfEmpty(OfferIndexRules.Clip(
                string.IsNullOrWhiteSpace(offer.Platform) ? offer.PlatformType?.ToString() : offer.Platform,
                64));
            row.Price = OfferIndexRules.RoundMoney(offer.Price);
            row.Shipping = NullIfEmpty(OfferIndexRules.Clip(offer.Shipping, 120));
            row.CommissionPercent = OfficialCatalogSearchRules.NormalizeCommissionPercent(offer.CommissionPercent);
            row.EstimatedCommission = OfferIndexRules.RoundMoney(offer.EstimatedCommission);
            row.IndexedAt = now;
            row.Touch();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasFreshTermAsync(
        int userId,
        string term,
        DateTime freshnessCutoffUtc,
        CancellationToken cancellationToken = default)
    {
        var needle = OfferIndexRules.NormalizeTerm(term);
        if (userId <= 0 || needle.Length < 2)
        {
            return false;
        }

        var rows = await _context.IndexedCatalogOffers
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.IndexedAt >= freshnessCutoffUtc)
            .OrderByDescending(item => item.IndexedAt)
            .Select(item => new { item.SearchTerm, item.ProductName, item.ProductModel })
            .Take(300)
            .ToListAsync(cancellationToken);
        return rows.Any(item => OfferIndexRules.MatchesTerm(needle, item.SearchTerm, item.ProductName, item.ProductModel));
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

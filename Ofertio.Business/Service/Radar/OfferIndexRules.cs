using Ofertio.Business.Dto;

namespace Ofertio.Business.Service.Radar;

/// <summary>Frescor do índice local (cache do Painel Master) e montagem da tabela do comparador.</summary>
public static class OfferIndexRules
{
    public const int InteractiveRefreshTermCap = 2;

    public const int WorkerRefreshTermCap = 20;

    public const int MiningSearchLimit = 8;

    public const int PromotePerCycle = 30;

    public const int MaxComparisonRows = 20;

    public static string NormalizeTerm(string? term) => term?.Trim().ToLowerInvariant() ?? string.Empty;

    public static string Clip(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || max <= 0)
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    public static decimal? RoundMoney(decimal? value) =>
        value is > 0 ? decimal.Round(value.Value, 2) : null;

    public static bool MatchesTerm(string normalizedNeedle, string? searchTerm, string? productName, string? productModel)
    {
        if (normalizedNeedle.Length < 2)
        {
            return false;
        }

        if (string.Equals(NormalizeTerm(searchTerm), normalizedNeedle, StringComparison.Ordinal))
        {
            return true;
        }

        if (productName?.Contains(normalizedNeedle, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return productModel?.Contains(normalizedNeedle, StringComparison.OrdinalIgnoreCase) == true;
    }

    public static bool BelongsToProfile(
        string? searchTerm,
        string? productName,
        string? productModel,
        IReadOnlyCollection<string> niches)
    {
        if (niches.Count == 0)
        {
            return true;
        }

        foreach (var niche in niches)
        {
            if (MatchesTerm(NormalizeTerm(niche), searchTerm, productName, productModel))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Cache 0 (desligado no Master) ou índice mais velho que a janela exigem nova consulta às APIs.</summary>
    public static bool NeedsOfficialRefresh(DateTime? newestIndexedAtUtc, DateTime utcNow, int cacheMinutes)
    {
        if (cacheMinutes <= 0 || newestIndexedAtUtc is null)
        {
            return true;
        }

        return newestIndexedAtUtc.Value < utcNow.AddMinutes(-cacheMinutes);
    }

    public static void MarkLowestPrice(IList<StorePriceComparisonRowDto> rows)
    {
        decimal? minimum = null;
        foreach (var row in rows)
        {
            row.IsLowestPrice = false;
            if (row.Price is > 0 && (minimum is null || row.Price < minimum))
            {
                minimum = row.Price;
            }
        }

        if (minimum is null)
        {
            return;
        }

        foreach (var row in rows)
        {
            row.IsLowestPrice = row.Price == minimum;
        }
    }

    public static List<StorePriceComparisonRowDto> BuildComparisonTable(
        StorePriceComparisonRowDto reference,
        IEnumerable<StorePriceComparisonRowDto> candidates,
        int maxRows)
    {
        var limit = maxRows < 1 ? 1 : maxRows;
        var rows = new List<StorePriceComparisonRowDto> { Clone(reference, isReference: true) };
        foreach (var candidate in candidates)
        {
            if (!ProductModelRules.IsComparableOffer(reference.ProductName, candidate.ProductName))
            {
                continue;
            }

            rows.Add(Clone(candidate, isReference: false));
        }

        var collapsed = CollapseByUrl(rows);
        var ordered = collapsed
            .OrderBy(row => row.Price is > 0 ? row.Price.Value : decimal.MaxValue)
            .ToList();
        var referenceRow = ordered.FirstOrDefault(row => row.IsReference);
        var visible = ordered.Where(row => !row.IsReference).Take(limit - (referenceRow is null ? 0 : 1)).ToList();
        if (referenceRow is not null)
        {
            visible.Add(referenceRow);
        }

        visible = visible
            .OrderBy(row => row.Price is > 0 ? row.Price.Value : decimal.MaxValue)
            .ToList();
        MarkLowestPrice(visible);
        return visible;
    }

    private static List<StorePriceComparisonRowDto> CollapseByUrl(IReadOnlyList<StorePriceComparisonRowDto> rows)
    {
        return rows
            .GroupBy(row => string.IsNullOrWhiteSpace(row.SourceUrl) ? "reference:" + row.ProductName : row.SourceUrl.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(Collapse)
            .ToList();
    }

    private static StorePriceComparisonRowDto Collapse(IGrouping<string, StorePriceComparisonRowDto> group)
    {
        var best = group
            .OrderBy(row => row.Price is > 0 ? 0 : 1)
            .ThenBy(row => row.Price ?? decimal.MaxValue)
            .First();
        var collapsed = Clone(best, group.Any(row => row.IsReference));
        collapsed.Shipping ??= group.Select(row => row.Shipping).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        collapsed.CommissionPercent ??= group.Select(row => row.CommissionPercent).FirstOrDefault(value => value is > 0);
        collapsed.EstimatedCommission ??= group.Select(row => row.EstimatedCommission).FirstOrDefault(value => value is > 0);
        if (string.IsNullOrWhiteSpace(collapsed.PlatformName))
        {
            collapsed.PlatformName = group.Select(row => row.PlatformName).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }

        return collapsed;
    }

    private static StorePriceComparisonRowDto Clone(StorePriceComparisonRowDto source, bool isReference) =>
        new()
        {
            ProductName = source.ProductName,
            ImageUrl = source.ImageUrl,
            SourceUrl = source.SourceUrl,
            PlatformType = source.PlatformType,
            PlatformName = source.PlatformName,
            Price = source.Price,
            Shipping = source.Shipping,
            CommissionPercent = source.CommissionPercent,
            EstimatedCommission = source.EstimatedCommission,
            IsReference = isReference
        };
}

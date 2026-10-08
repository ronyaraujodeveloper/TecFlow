namespace TecFlow.Business.Service.Radar;

public static class PriceHistoryRules
{
    public static readonly int[] WindowsDays = [30, 60, 90];

    public static decimal? MinInWindow(IReadOnlyList<decimal> prices, IReadOnlyList<DateTime> capturedAt, DateTime utcNow, int days)
    {
        var cutoff = utcNow.AddDays(-days);
        decimal? min = null;
        for (var i = 0; i < prices.Count; i++)
        {
            if (capturedAt[i] < cutoff || prices[i] <= 0)
            {
                continue;
            }

            min = min is null ? prices[i] : Math.Min(min.Value, prices[i]);
        }

        return min;
    }

    public static decimal? AverageInWindow(IReadOnlyList<decimal> prices, IReadOnlyList<DateTime> capturedAt, DateTime utcNow, int days)
    {
        var cutoff = utcNow.AddDays(-days);
        decimal sum = 0;
        var count = 0;
        for (var i = 0; i < prices.Count; i++)
        {
            if (capturedAt[i] < cutoff || prices[i] <= 0)
            {
                continue;
            }

            sum += prices[i];
            count++;
        }

        return count == 0 ? null : Math.Round(sum / count, 2);
    }

    public static bool IsLowestInWindow(decimal currentPrice, decimal? windowMin)
    {
        if (currentPrice <= 0 || windowMin is not > 0)
        {
            return false;
        }

        return currentPrice <= windowMin.Value + 0.009m;
    }

    public static decimal? SavingsVersusAverage(decimal currentPrice, decimal? average)
    {
        if (currentPrice <= 0 || average is not > 0 || currentPrice >= average.Value)
        {
            return null;
        }

        return Math.Round(average.Value - currentPrice, 2);
    }

    public static bool ShouldSkipDuplicate(decimal lastPrice, DateTime lastCapturedAt, decimal newPrice, DateTime utcNow) =>
        lastPrice == newPrice && utcNow - lastCapturedAt < TimeSpan.FromHours(6);
}

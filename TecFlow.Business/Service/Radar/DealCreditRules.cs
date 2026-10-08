namespace TecFlow.Business.Service.Radar;

public static class DealCreditRules
{
    public const int DailyQuota = 5;
    public const decimal PriceDropThresholdPercent = 15m;
    public const int MinEngagement = 2;
    public const int UnlockCost = 1;

    public static readonly int[] TopUpPacks = [10, 50, 100];

    public const string KindDailyGrant = "DailyGrant";
    public const string KindTopUp = "TopUp";
    public const string KindUnlock = "Unlock";

    public static bool IsBasicPlan(string? plan)
    {
        if (string.IsNullOrWhiteSpace(plan))
        {
            return true;
        }

        var normalized = plan.Trim();
        return normalized.Equals("Free", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Basico", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Básico", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Basic", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ShouldGrantDaily(DateTime? lastGrantedUtc, DateTime utcNow)
    {
        var today = utcNow.Date;
        if (lastGrantedUtc is null)
        {
            return true;
        }

        return lastGrantedUtc.Value.ToUniversalTime().Date < today;
    }

    public static DateTime DailyExpiry(DateTime utcNow) => utcNow.Date.AddDays(1);

    public static decimal PriceDropPercent(decimal previousPrice, decimal currentPrice)
    {
        if (previousPrice <= 0 || currentPrice <= 0 || currentPrice >= previousPrice)
        {
            return 0;
        }

        return Math.Round((previousPrice - currentPrice) / previousPrice * 100m, 2);
    }

    public static bool IsSuperAchado(decimal dropPercent, int engagementCount) =>
        dropPercent >= PriceDropThresholdPercent && engagementCount >= MinEngagement;

    public static bool TryConsume(int dailyBalance, int purchasedBalance, int cost, out int newDaily, out int newPurchased)
    {
        newDaily = dailyBalance;
        newPurchased = purchasedBalance;
        if (cost <= 0)
        {
            return true;
        }

        var remaining = cost;
        var fromDaily = Math.Min(newDaily, remaining);
        newDaily -= fromDaily;
        remaining -= fromDaily;
        if (remaining <= 0)
        {
            return true;
        }

        if (newPurchased < remaining)
        {
            return false;
        }

        newPurchased -= remaining;
        return true;
    }

    public static int TotalBalance(int dailyBalance, int purchasedBalance) =>
        Math.Max(0, dailyBalance) + Math.Max(0, purchasedBalance);

    public static bool IsValidPack(int packSize) => TopUpPacks.Contains(packSize);
}

namespace TecFlow.Business.Dto;

public class PriceHistoryInsightDto
{
    public bool IsLowestPrice30Days { get; set; }

    public decimal? MinPrice30Days { get; set; }

    public decimal? AveragePrice30Days { get; set; }

    public decimal? SavingsVersusAverage { get; set; }
}

public class TenantCreditsDto
{
    public int DailyBalance { get; set; }

    public int PurchasedBalance { get; set; }

    public int TotalBalance { get; set; }

    public DateTime? DailyGrantedOnUtc { get; set; }

    public DateTime? DailyExpiresAtUtc { get; set; }
}

public class GlobalTrendingDealDto
{
    public int Id { get; set; }

    public string Platform { get; set; } = string.Empty;

    public string PlatformProductId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string? ProductImageUrl { get; set; }

    public string? OriginalUrl { get; set; }

    public decimal CurrentPrice { get; set; }

    public decimal? PreviousPrice { get; set; }

    public decimal PriceDropPercent { get; set; }

    public int EngagementCount { get; set; }

    public bool IsUnlocked { get; set; }

    public bool IsSuperAchado { get; set; }

    public DateTime LastSeenAt { get; set; }
}

public class DealCreditsResponseDto
{
    public bool Status { get; set; } = true;

    public string Descricao { get; set; } = string.Empty;

    public TenantCreditsDto Credits { get; set; } = new();

    public List<GlobalTrendingDealDto> Items { get; set; } = [];
}

public class TopUpCreditsRequestDto
{
    public int PackSize { get; set; }
}

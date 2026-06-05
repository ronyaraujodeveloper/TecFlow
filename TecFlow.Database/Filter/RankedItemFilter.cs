namespace TecFlow.Database.Filter;

public class RankedItemFilter
{
    public int? ItemId { get; set; }
    public decimal? Score { get; set; }
    public string? Name { get; set; }
    public int? OwnerId { get; set; }
}

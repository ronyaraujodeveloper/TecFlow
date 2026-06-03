namespace TecFlow.Database.Filter;

public class MetricFilter
{
    public int? Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? CampaignId { get; set; }
    public int? Views { get; set; }
    public int? Clicks { get; set; }
    public int? Sales { get; set; }
    public decimal? Investment { get; set; }
    public decimal? Revenue { get; set; }
    public int? OwnerId { get; set; }
    public int? ParentMetricId { get; set; }
}

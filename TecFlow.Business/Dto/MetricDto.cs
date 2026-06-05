namespace TecFlow.Business.Dto;

public class MetricDto
{
    public int CampaignId { get; set; }
    public int Views { get; set; }
    public int Clicks { get; set; }
    public int Sales { get; set; }
    public decimal Investment { get; set; }
    public decimal Revenue { get; set; }
    public int? ParentMetricId { get; set; }
}

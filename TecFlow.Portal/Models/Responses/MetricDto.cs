namespace TecFlow.Portal.Models.Responses;

public class MetricDto
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public int Views { get; set; }
    public int Clicks { get; set; }
    public int Sales { get; set; }
    public decimal Investment { get; set; }
    public decimal Revenue { get; set; }
    public int OwnerId { get; set; }

    public decimal Profit => Revenue - Investment;

    public double Ctr => Views == 0 ? 0 : (double)Clicks / Views * 100;

    public double Conversion => Clicks == 0 ? 0 : (double)Sales / Clicks * 100;

    public double Roi => Investment == 0 ? 0 : (double)(Profit / Investment) * 100;
}

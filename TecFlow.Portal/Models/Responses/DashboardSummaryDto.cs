namespace TecFlow.Portal.Models.Responses;

public class DashboardSummaryDto
{
    public long TotalViews { get; set; }
    public long TotalClicks { get; set; }
    public long TotalSales { get; set; }
    public decimal TotalInvestment { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalProfit { get; set; }
    public double AverageCtr { get; set; }
    public double AverageConversion { get; set; }
    public decimal AverageRoi { get; set; }
    public decimal AverageCac { get; set; }
}

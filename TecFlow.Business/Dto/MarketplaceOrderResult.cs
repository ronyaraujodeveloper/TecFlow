namespace TecFlow.Business.Dto;

public class MarketplaceOrderResult
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? ExternalOrderId { get; set; }
    public bool AlreadyProcessed { get; set; }
    public int LinesProcessed { get; set; }
}

namespace TecFlow.Business.Dto;

public class InventoryDto
{
    public int ProductId { get; set; }
    public int MinimumStock { get; set; }
}

public class InventoryAdjustmentDto
{
    public int ProductId { get; set; }
    public int QuantityDelta { get; set; }
    public string? Description { get; set; }
}

public class InventoryPurchaseEntryDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Description { get; set; }
}

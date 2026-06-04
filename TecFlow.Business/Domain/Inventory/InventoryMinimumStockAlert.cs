namespace TecFlow.Business.Domain.Inventory;

/// <summary>Evento de alerta quando o estoque disponível fica abaixo do mínimo configurado.</summary>
public sealed class InventoryMinimumStockAlert
{
    public Guid TenantId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int AvailableQuantity { get; init; }
    public int MinimumStock { get; init; }
    public Guid? SalesOrderId { get; init; }
    public string Trigger { get; init; } = string.Empty;
}

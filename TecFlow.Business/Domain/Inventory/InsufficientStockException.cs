namespace TecFlow.Business.Domain.Inventory;

public class InsufficientStockException : InvalidOperationException
{
    public int ProductId { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public InsufficientStockException(int productId, int requested, int available)
        : base($"Estoque insuficiente para o produto {productId}. Solicitado: {requested}, disponível: {available}.")
    {
        ProductId = productId;
        RequestedQuantity = requested;
        AvailableQuantity = available;
    }
}

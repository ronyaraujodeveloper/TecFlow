namespace TecFlow.Business.Interfaces.Inventory;

public interface IInventoryService
{
    Task<global::TecFlow.Core.Entities.Inventory> GetOrCreateAsync(int productId, CancellationToken cancellationToken = default);

    Task ReserveStockAsync(int productId, int quantity, Guid orderId, CancellationToken cancellationToken = default);

    Task ReserveStockForOrderAsync(
        IReadOnlyList<(int ProductId, int Quantity)> lines,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task ConfirmStockDebitAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task ReleaseStockReservationAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<global::TecFlow.Core.Entities.Inventory> RegisterPurchaseEntryAsync(
        int productId,
        int quantity,
        string? description = null,
        CancellationToken cancellationToken = default);

    Task<global::TecFlow.Core.Entities.Inventory> RegisterManualAdjustmentAsync(
        int productId,
        int quantityDelta,
        string? description = null,
        CancellationToken cancellationToken = default);
}

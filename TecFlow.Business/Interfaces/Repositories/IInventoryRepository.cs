using TecFlow.Core.Enums;

namespace TecFlow.Business.Interfaces.Repositories;

public interface IInventoryRepository
{
    Task<global::TecFlow.Core.Entities.Inventory?> GetByProductIdAsync(int productId, bool forUpdate = false);
    Task<global::TecFlow.Core.Entities.Inventory> AddAsync(global::TecFlow.Core.Entities.Inventory inventory);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> HasMovementForOrderAsync(Guid orderId, InventoryMovementType movementType);
}

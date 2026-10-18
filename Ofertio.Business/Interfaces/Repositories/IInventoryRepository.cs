using Ofertio.Core.Enums;

namespace Ofertio.Business.Interfaces.Repositories;

public interface IInventoryRepository
{
    Task<global::Ofertio.Core.Entities.Inventory?> GetByProductIdAsync(int productId, bool forUpdate = false);
    Task<global::Ofertio.Core.Entities.Inventory> AddAsync(global::Ofertio.Core.Entities.Inventory inventory);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> HasMovementForOrderAsync(Guid orderId, InventoryMovementType movementType);
}

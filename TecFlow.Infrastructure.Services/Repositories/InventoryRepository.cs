using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Enums;
using TecFlow.Database;
using InventoryEntity = TecFlow.Core.Entities.Inventory;

namespace TecFlow.Infrastructure.Services.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _context;

    public InventoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<InventoryEntity?> GetByProductIdAsync(int productId, bool forUpdate = false) =>
        _context.Inventories
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductId == productId);

    public async Task<InventoryEntity> AddAsync(InventoryEntity inventory)
    {
        inventory.CreatedAt = DateTime.UtcNow;
        await _context.Inventories.AddAsync(inventory);
        return inventory;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public Task<bool> HasMovementForOrderAsync(Guid orderId, InventoryMovementType movementType) =>
        _context.InventoryMovements.AnyAsync(m =>
            m.SalesOrderId == orderId && m.MovementType == movementType);
}

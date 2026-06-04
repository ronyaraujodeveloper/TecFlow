using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Domain.Inventory;
using TecFlow.Business.Interfaces.Inventory;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;
using InventoryEntity = TecFlow.Core.Entities.Inventory;

namespace TecFlow.Infrastructure.Services.Stock;

public class InventoryService : IInventoryService
{
    private const int MaxConcurrencyRetries = 4;

    private readonly AppDbContext _context;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICurrentTenantService _currentTenant;
    private readonly IInventoryAlertHook _alertHook;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        AppDbContext context,
        IInventoryRepository inventoryRepository,
        ICurrentTenantService currentTenant,
        IInventoryAlertHook alertHook,
        ILogger<InventoryService> logger)
    {
        _context = context;
        _inventoryRepository = inventoryRepository;
        _currentTenant = currentTenant;
        _alertHook = alertHook;
        _logger = logger;
    }

    public async Task<InventoryEntity> GetOrCreateAsync(int productId, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var existing = await _inventoryRepository.GetByProductIdAsync(productId);
        if (existing is not null)
        {
            return existing;
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new InvalidOperationException($"Produto {productId} não encontrado.");

        var inventory = new InventoryEntity
        {
            TenantId = tenantId,
            ProductId = productId,
            PhysicalQuantity = Math.Max(0, product.Stock),
            ReservedQuantity = 0,
            MinimumStock = 0
        };

        await _inventoryRepository.AddAsync(inventory);
        await _inventoryRepository.SaveChangesAsync(cancellationToken);
        return inventory;
    }

    public Task ReserveStockAsync(int productId, int quantity, Guid orderId, CancellationToken cancellationToken = default)
    {
        return ReserveStockForOrderAsync([(productId, quantity)], orderId, cancellationToken);
    }

    public Task ReserveStockForOrderAsync(
        IReadOnlyList<(int ProductId, int Quantity)> lines,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
        {
            throw new ArgumentException("O pedido deve conter itens para reservar estoque.", nameof(lines));
        }

        return ExecuteWithConcurrencyRetryAsync(async ct =>
        {
            foreach (var (productId, quantity) in lines)
            {
                if (quantity <= 0)
                {
                    throw new ArgumentException($"Quantidade inválida para o produto {productId}.");
                }

                var inventory = await LoadInventoryForUpdateAsync(productId, ct);

                if (inventory.AvailableQuantity < quantity)
                {
                    throw new InsufficientStockException(productId, quantity, inventory.AvailableQuantity);
                }

                inventory.ReservedQuantity += quantity;
                inventory.Touch();

                await AddMovementAsync(
                    inventory,
                    quantity,
                    InventoryMovementType.Reserva,
                    $"Reserva para pedido {orderId}",
                    orderId,
                    ct);

                await SyncProductStockAsync(inventory, ct);
                await CheckMinimumStockAsync(inventory, orderId, "ReserveStock", ct);
            }
        }, cancellationToken);
    }

    public Task ConfirmStockDebitAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return ExecuteWithConcurrencyRetryAsync(async ct =>
        {
            if (await _inventoryRepository.HasMovementForOrderAsync(orderId, InventoryMovementType.SaidaPorVenda))
            {
                _logger.LogDebug("Débito de estoque já confirmado para o pedido {OrderId}.", orderId);
                return;
            }

            var order = await _context.SalesOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct)
                ?? throw new InvalidOperationException($"Pedido '{orderId}' não encontrado.");

            foreach (var line in order.Items)
            {
                var inventory = await LoadInventoryForUpdateAsync(line.ProductId, ct);

                if (inventory.ReservedQuantity < line.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Reserva insuficiente no produto {line.ProductId} para o pedido {orderId}.");
                }

                inventory.ReservedQuantity -= line.Quantity;
                inventory.PhysicalQuantity = Math.Max(0, inventory.PhysicalQuantity - line.Quantity);
                inventory.Touch();

                await AddMovementAsync(
                    inventory,
                    line.Quantity,
                    InventoryMovementType.SaidaPorVenda,
                    $"Saída por venda — pedido {order.OrderNumber}",
                    orderId,
                    ct);

                await SyncProductStockAsync(inventory, ct);
                await CheckMinimumStockAsync(inventory, orderId, "ConfirmStockDebit", ct);
            }
        }, cancellationToken);
    }

    public Task ReleaseStockReservationAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return ExecuteWithConcurrencyRetryAsync(async ct =>
        {
            if (await _inventoryRepository.HasMovementForOrderAsync(orderId, InventoryMovementType.CancelamentoReserva))
            {
                _logger.LogDebug("Reserva já liberada para o pedido {OrderId}.", orderId);
                return;
            }

            var order = await _context.SalesOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct)
                ?? throw new InvalidOperationException($"Pedido '{orderId}' não encontrado.");

            var hasReservation = await _context.InventoryMovements.AnyAsync(
                m => m.SalesOrderId == orderId && m.MovementType == InventoryMovementType.Reserva,
                ct);

            if (!hasReservation)
            {
                return;
            }

            foreach (var line in order.Items)
            {
                var inventory = await LoadInventoryForUpdateAsync(line.ProductId, ct);
                var releaseQty = Math.Min(line.Quantity, inventory.ReservedQuantity);
                if (releaseQty <= 0)
                {
                    continue;
                }

                inventory.ReservedQuantity -= releaseQty;
                inventory.Touch();

                await AddMovementAsync(
                    inventory,
                    releaseQty,
                    InventoryMovementType.CancelamentoReserva,
                    $"Cancelamento de reserva — pedido {order.OrderNumber}",
                    orderId,
                    ct);

                await CheckMinimumStockAsync(inventory, orderId, "ReleaseReservation", ct);
            }
        }, cancellationToken);
    }

    public Task<InventoryEntity> RegisterPurchaseEntryAsync(
        int productId,
        int quantity,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantidade de entrada deve ser maior que zero.", nameof(quantity));
        }

        return ExecuteWithConcurrencyRetryAsync(async ct =>
        {
            var inventory = await LoadInventoryForUpdateAsync(productId, ct);
            inventory.PhysicalQuantity += quantity;
            inventory.Touch();

            await AddMovementAsync(
                inventory,
                quantity,
                InventoryMovementType.EntradaPorCompra,
                description ?? "Entrada por compra",
                null,
                ct);

            await SyncProductStockAsync(inventory, ct);
            return inventory;
        }, cancellationToken);
    }

    public Task<InventoryEntity> RegisterManualAdjustmentAsync(
        int productId,
        int quantityDelta,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (quantityDelta == 0)
        {
            throw new ArgumentException("Ajuste manual requer quantidade diferente de zero.", nameof(quantityDelta));
        }

        return ExecuteWithConcurrencyRetryAsync(async ct =>
        {
            var inventory = await LoadInventoryForUpdateAsync(productId, ct);

            if (quantityDelta < 0 && inventory.AvailableQuantity < Math.Abs(quantityDelta))
            {
                throw new InsufficientStockException(
                    productId,
                    Math.Abs(quantityDelta),
                    inventory.AvailableQuantity);
            }

            inventory.PhysicalQuantity = Math.Max(0, inventory.PhysicalQuantity + quantityDelta);
            inventory.Touch();

            await AddMovementAsync(
                inventory,
                Math.Abs(quantityDelta),
                InventoryMovementType.AjusteManual,
                description ?? $"Ajuste manual ({quantityDelta:+0;-0})",
                null,
                ct);

            await SyncProductStockAsync(inventory, ct);
            await CheckMinimumStockAsync(inventory, null, "ManualAdjustment", ct);
            return inventory;
        }, cancellationToken);
    }

    private async Task<InventoryEntity> LoadInventoryForUpdateAsync(int productId, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();

        var inventory = await _context.Inventories
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.TenantId == tenantId, cancellationToken);

        if (inventory is not null)
        {
            return inventory;
        }

        return await GetOrCreateAsync(productId, cancellationToken);
    }

    private async Task AddMovementAsync(
        InventoryEntity inventory,
        int quantity,
        InventoryMovementType type,
        string description,
        Guid? salesOrderId,
        CancellationToken cancellationToken)
    {
        await _context.InventoryMovements.AddAsync(new InventoryMovement
        {
            TenantId = inventory.TenantId,
            InventoryId = inventory.Id,
            Quantity = quantity,
            MovementType = type,
            Description = description,
            SalesOrderId = salesOrderId,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _inventoryRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncProductStockAsync(InventoryEntity inventory, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == inventory.ProductId, cancellationToken);
        if (product is null)
        {
            return;
        }

        product.Stock = inventory.PhysicalQuantity;
        product.ModifiedOn = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        await _inventoryRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task CheckMinimumStockAsync(
        InventoryEntity inventory,
        Guid? salesOrderId,
        string trigger,
        CancellationToken cancellationToken)
    {
        if (inventory.MinimumStock <= 0 || inventory.AvailableQuantity >= inventory.MinimumStock)
        {
            return;
        }

        var alert = new InventoryMinimumStockAlert
        {
            TenantId = inventory.TenantId,
            ProductId = inventory.ProductId,
            ProductName = inventory.Product?.Name ?? $"Produto #{inventory.ProductId}",
            AvailableQuantity = inventory.AvailableQuantity,
            MinimumStock = inventory.MinimumStock,
            SalesOrderId = salesOrderId,
            Trigger = trigger
        };

        await _alertHook.OnMinimumStockReachedAsync(alert, cancellationToken);
    }

    private Guid RequireTenantId()
    {
        if (_currentTenant.TenantId is null)
        {
            throw new InvalidOperationException("Tenant não identificado no contexto da requisição.");
        }

        return _currentTenant.TenantId.Value;
    }

    private async Task ExecuteWithConcurrencyRetryAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var ownsTransaction = _context.Database.CurrentTransaction is null;
            await using var transaction = ownsTransaction
                ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                : null;

            try
            {
                await action(cancellationToken);
                if (ownsTransaction && transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return;
            }
            catch (Exception ex) when (attempt < MaxConcurrencyRetries && IsConcurrencyConflict(ex))
            {
                if (ownsTransaction && transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                _context.ChangeTracker.Clear();
                _logger.LogWarning(ex, "Conflito de concorrência no estoque (tentativa {Attempt}).", attempt);
            }
        }
    }

    private async Task<InventoryEntity> ExecuteWithConcurrencyRetryAsync(
        Func<CancellationToken, Task<InventoryEntity>> action,
        CancellationToken cancellationToken)
    {
        InventoryEntity? result = null;
        await ExecuteWithConcurrencyRetryAsync(async ct => result = await action(ct), cancellationToken);
        return result!;
    }

    private static bool IsConcurrencyConflict(Exception ex) =>
        ex is DbUpdateConcurrencyException or DbUpdateException;
}

using TecFlow.Business.Domain.Inventory;

namespace TecFlow.Business.Interfaces.Inventory;

/// <summary>Gancho para alertas futuros (push Fase 4) quando estoque atinge nível mínimo.</summary>
public interface IInventoryAlertHook
{
    Task OnMinimumStockReachedAsync(InventoryMinimumStockAlert alert, CancellationToken cancellationToken = default);
}

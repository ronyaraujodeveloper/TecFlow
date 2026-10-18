using Microsoft.Extensions.Logging;
using Ofertio.Business.Domain.Inventory;
using Ofertio.Business.Interfaces.Inventory;

namespace Ofertio.Infrastructure.Services.Stock;

/// <summary>Registra alertas estruturados; ponto de extensão para push (Fase 4).</summary>
public class LoggingInventoryAlertHook : IInventoryAlertHook
{
    private readonly ILogger<LoggingInventoryAlertHook> _logger;

    public LoggingInventoryAlertHook(ILogger<LoggingInventoryAlertHook> logger)
    {
        _logger = logger;
    }

    public Task OnMinimumStockReachedAsync(InventoryMinimumStockAlert alert, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Estoque abaixo do mínimo. TenantId={TenantId} ProductId={ProductId} ProductName={ProductName} " +
            "Available={AvailableQuantity} Minimum={MinimumStock} OrderId={SalesOrderId} Trigger={Trigger}",
            alert.TenantId,
            alert.ProductId,
            alert.ProductName,
            alert.AvailableQuantity,
            alert.MinimumStock,
            alert.SalesOrderId,
            alert.Trigger);

        return Task.CompletedTask;
    }
}

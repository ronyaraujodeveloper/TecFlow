using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Interfaces.Repositories;

public interface ISalesOrderRepository
{
    Task<SalesOrder?> GetByIdAsync(Guid id, bool includeItems = false);
    Task<IReadOnlyList<SalesOrder>> ListAsync();
    Task<string> GenerateNextOrderNumberAsync(Guid tenantId);
    Task<SalesOrder> CreateAsync(SalesOrder order);
    Task<SalesOrder> UpdateAsync(SalesOrder order);
    Task<SalesOrder> UpdateStatusAsync(Guid orderId, OrderStatus status);
}

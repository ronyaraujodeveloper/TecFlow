using TecFlow.Business.Dto;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Interfaces.Sales;

public interface ISalesOrderService
{
    Task<SalesOrder> CreateOrderAsync(SalesOrderDto dto, CancellationToken cancellationToken = default);
    Task<SalesOrder> TransitionStatusAsync(Guid orderId, OrderStatus targetStatus, CancellationToken cancellationToken = default);
}

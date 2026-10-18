using Ofertio.Business.Dto;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Interfaces.Sales;

public interface ISalesOrderService
{
    Task<SalesOrder> CreateOrderAsync(SalesOrderDto dto, CancellationToken cancellationToken = default);
    Task<SalesOrder> TransitionStatusAsync(Guid orderId, OrderStatus targetStatus, CancellationToken cancellationToken = default);
}

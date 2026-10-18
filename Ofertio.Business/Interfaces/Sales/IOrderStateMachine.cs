using Ofertio.Core.Enums;

namespace Ofertio.Business.Interfaces.Sales;

public interface IOrderStateMachine
{
    bool CanTransition(OrderStatus current, OrderStatus target);

    void EnsureCanTransition(OrderStatus current, OrderStatus target);
}

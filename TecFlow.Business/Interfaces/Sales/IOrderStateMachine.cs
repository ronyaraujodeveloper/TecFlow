using TecFlow.Core.Enums;

namespace TecFlow.Business.Interfaces.Sales;

public interface IOrderStateMachine
{
    bool CanTransition(OrderStatus current, OrderStatus target);

    void EnsureCanTransition(OrderStatus current, OrderStatus target);
}

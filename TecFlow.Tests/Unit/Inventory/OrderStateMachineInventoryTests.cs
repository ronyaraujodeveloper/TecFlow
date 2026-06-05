using TecFlow.Business.Domain.Sales;

namespace TecFlow.Tests.Unit.Inventory;

public class OrderStateMachineInventoryTests
{
    private readonly OrderStateMachine _machine = new();

    [Fact]
    public void Cancelado_CannotTransitionTo_Enviado()
    {
        Assert.False(_machine.CanTransition(
            TecFlow.Core.Enums.OrderStatus.Cancelado,
            TecFlow.Core.Enums.OrderStatus.Enviado));
    }
}

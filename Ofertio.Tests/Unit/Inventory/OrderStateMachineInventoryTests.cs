using Ofertio.Business.Domain.Sales;

namespace Ofertio.Tests.Unit.Inventory;

public class OrderStateMachineInventoryTests
{
    private readonly OrderStateMachine _machine = new();

    [Fact]
    public void Cancelado_CannotTransitionTo_Enviado()
    {
        Assert.False(_machine.CanTransition(
            Ofertio.Core.Enums.OrderStatus.Cancelado,
            Ofertio.Core.Enums.OrderStatus.Enviado));
    }
}

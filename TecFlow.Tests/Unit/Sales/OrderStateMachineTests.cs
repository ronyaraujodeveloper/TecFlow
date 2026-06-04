using TecFlow.Business.Domain.Sales;
using TecFlow.Core.Enums;

namespace TecFlow.Tests.Unit.Sales;

public class OrderStateMachineTests
{
    private readonly OrderStateMachine _machine = new();

    [Theory]
    [InlineData(OrderStatus.Pendente, OrderStatus.Pago, true)]
    [InlineData(OrderStatus.Pago, OrderStatus.Faturado, true)]
    [InlineData(OrderStatus.Pendente, OrderStatus.Faturado, false)]
    [InlineData(OrderStatus.Cancelado, OrderStatus.Enviado, false)]
    [InlineData(OrderStatus.Concluido, OrderStatus.Pago, false)]
    public void CanTransition_ReturnsExpected(OrderStatus current, OrderStatus target, bool expected)
    {
        Assert.Equal(expected, _machine.CanTransition(current, target));
    }

    [Fact]
    public void EnsureCanTransition_Throws_WhenInvalid()
    {
        Assert.Throws<OrderTransitionException>(() =>
            _machine.EnsureCanTransition(OrderStatus.Cancelado, OrderStatus.Enviado));
    }
}

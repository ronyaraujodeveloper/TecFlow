using TecFlow.Business.Interfaces.Sales;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Domain.Sales;

/// <summary>Validações rígidas de transição do ciclo de vida do pedido de venda.</summary>
public class OrderStateMachine : IOrderStateMachine
{
    private static readonly IReadOnlyDictionary<OrderStatus, HashSet<OrderStatus>> AllowedTransitions =
        new Dictionary<OrderStatus, HashSet<OrderStatus>>
        {
            [OrderStatus.Pendente] = [OrderStatus.Pago, OrderStatus.Cancelado],
            [OrderStatus.Pago] = [OrderStatus.Faturado, OrderStatus.Cancelado],
            [OrderStatus.Faturado] = [OrderStatus.Enviado, OrderStatus.Cancelado],
            [OrderStatus.Enviado] = [OrderStatus.Concluido, OrderStatus.Cancelado],
            [OrderStatus.Concluido] = [],
            [OrderStatus.Cancelado] = []
        };

    public bool CanTransition(OrderStatus current, OrderStatus target)
    {
        if (current == target)
        {
            return true;
        }

        return AllowedTransitions.TryGetValue(current, out var allowed) && allowed.Contains(target);
    }

    public void EnsureCanTransition(OrderStatus current, OrderStatus target)
    {
        if (current == target)
        {
            return;
        }

        if (!CanTransition(current, target))
        {
            throw new OrderTransitionException(
                $"Transição inválida de '{current}' para '{target}'. " +
                $"Um pedido faturado exige status 'Pago'; pedidos cancelados ou concluídos não podem avançar.");
        }
    }
}

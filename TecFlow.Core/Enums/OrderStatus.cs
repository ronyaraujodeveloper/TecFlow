namespace TecFlow.Core.Enums;

/// <summary>Ciclo de vida do pedido de venda direta (ERP local).</summary>
public enum OrderStatus
{
    Pendente = 1,
    Pago = 2,
    Faturado = 3,
    Enviado = 4,
    Concluido = 5,
    Cancelado = 6
}

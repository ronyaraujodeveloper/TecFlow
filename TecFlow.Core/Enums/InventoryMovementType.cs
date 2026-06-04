namespace TecFlow.Core.Enums;

/// <summary>Tipo de movimentação no kardex de estoque físico.</summary>
public enum InventoryMovementType
{
    EntradaPorCompra = 1,
    SaidaPorVenda = 2,
    AjusteManual = 3,
    Reserva = 4,
    CancelamentoReserva = 5
}

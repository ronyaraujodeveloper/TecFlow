using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Sales;

public interface IInvoiceOrchestrator
{
    /// <summary>
    /// Prepara emissão de NF-e: valida estado, transiciona para Faturado e retorna payload mockado para gateway futuro.
    /// </summary>
    Task<InvoicePreparationResponseDto> PrepareInvoiceAsync(Guid orderId, CancellationToken cancellationToken = default);
}

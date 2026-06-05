using TecFlow.Business.Domain.Sales;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Inventory;
using TecFlow.Business.Interfaces.Sales;
using TecFlow.Core.Enums;
using TecFlow.Database;
using Microsoft.EntityFrameworkCore;

namespace TecFlow.Infrastructure.Services.Sales;

public class InvoiceOrchestrator : IInvoiceOrchestrator
{
    private readonly ISalesOrderRepository _orderRepository;
    private readonly IOrderStateMachine _stateMachine;
    private readonly AppDbContext _context;
    private readonly IInventoryService _inventoryService;

    public InvoiceOrchestrator(
        ISalesOrderRepository orderRepository,
        IOrderStateMachine stateMachine,
        IInventoryService inventoryService,
        AppDbContext context)
    {
        _orderRepository = orderRepository;
        _stateMachine = stateMachine;
        _inventoryService = inventoryService;
        _context = context;
    }

    public async Task<InvoicePreparationResponseDto> PrepareInvoiceAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, includeItems: true)
            ?? throw new InvalidOperationException($"Pedido '{orderId}' não encontrado.");

        if (order.Status != OrderStatus.Pago)
        {
            return new InvoicePreparationResponseDto
            {
                Status = false,
                Descricao = "Somente pedidos com status 'Pago' podem ser faturados."
            };
        }

        _stateMachine.EnsureCanTransition(order.Status, OrderStatus.Faturado);

        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == order.TenantId, cancellationToken);

        var customer = order.Customer
            ?? throw new InvalidOperationException("Cliente do pedido não carregado.");

        var payload = new InvoicePayloadDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            IssuedAt = DateTime.UtcNow,
            Issuer = new InvoiceIssuerDto
            {
                TenantId = order.TenantId,
                TenantName = tenant?.Name ?? "Tenant",
                DocumentNumber = tenant?.DocumentNumber
            },
            Customer = new InvoiceCustomerDto
            {
                CustomerId = customer.Id,
                Name = customer.Name,
                DocumentNumber = customer.DocumentNumber,
                Email = customer.Email,
                DeliveryAddress = FormatAddress(customer)
            },
            Lines = order.Items.Select(i => new InvoiceLineDto
            {
                ProductId = i.ProductId,
                Description = i.Product?.Name ?? $"Produto #{i.ProductId}",
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList(),
            Totals = new InvoiceTotalsDto
            {
                Subtotal = order.Items.Sum(i => i.TotalPrice),
                DiscountAmount = order.DiscountAmount,
                FreightAmount = order.FreightAmount,
                TotalAmount = order.TotalAmount
            }
        };

        await _inventoryService.ConfirmStockDebitAsync(orderId, cancellationToken);
        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Faturado);

        return new InvoicePreparationResponseDto
        {
            Status = true,
            Descricao = "Pedido faturado. Payload NF-e mockado pronto para gateway/mensageria.",
            Data = payload
        };
    }

    private static string FormatAddress(Core.Entities.Customer customer) =>
        $"{customer.Street}, {customer.StreetNumber} — {customer.ZipCode} — {customer.City}/{customer.State}";
}

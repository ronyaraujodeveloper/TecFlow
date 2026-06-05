using System.Data;
using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Domain.Inventory;
using TecFlow.Business.Domain.Sales;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Inventory;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Sales;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;

namespace TecFlow.Infrastructure.Services.Sales;

public class SalesOrderService : ISalesOrderService
{
    private readonly AppDbContext _context;
    private readonly ISalesOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IOrderStateMachine _stateMachine;
    private readonly IInventoryService _inventoryService;
    private readonly ICurrentTenantService _currentTenant;

    public SalesOrderService(
        AppDbContext context,
        ISalesOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IOrderStateMachine stateMachine,
        IInventoryService inventoryService,
        ICurrentTenantService currentTenant)
    {
        _context = context;
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _stateMachine = stateMachine;
        _inventoryService = inventoryService;
        _currentTenant = currentTenant;
    }

    public async Task<SalesOrder> CreateOrderAsync(SalesOrderDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentTenant.TenantId is null)
        {
            throw new InvalidOperationException("Tenant não identificado no contexto da requisição.");
        }

        if (dto.Items.Count == 0)
        {
            throw new ArgumentException("O pedido deve conter ao menos um item.");
        }

        var customer = await _customerRepository.GetByIdAsync(dto.CustomerId)
            ?? throw new InvalidOperationException($"Cliente {dto.CustomerId} não encontrado.");

        var tenantId = _currentTenant.TenantId.Value;
        var orderNumber = await _orderRepository.GenerateNextOrderNumberAsync(tenantId);

        var order = new SalesOrder
        {
            TenantId = tenantId,
            ShopId = dto.ShopId,
            CustomerId = customer.Id,
            OrderNumber = orderNumber,
            DiscountAmount = dto.DiscountAmount,
            FreightAmount = dto.FreightAmount,
            Status = OrderStatus.Pendente
        };

        decimal subtotal = 0;
        foreach (var line in dto.Items)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == line.ProductId, cancellationToken)
                ?? throw new InvalidOperationException($"Produto {line.ProductId} não encontrado.");

            var totalLine = line.UnitPrice * line.Quantity;
            subtotal += totalLine;

            order.Items.Add(new SalesOrderItem
            {
                TenantId = tenantId,
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                TotalPrice = totalLine
            });
        }

        order.TotalAmount = subtotal - order.DiscountAmount + order.FreightAmount;
        if (order.TotalAmount < 0)
        {
            order.TotalAmount = 0;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            order.CreatedAt = DateTime.UtcNow;
            await _context.SalesOrders.AddAsync(order, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var lines = order.Items.Select(i => (i.ProductId, i.Quantity)).ToList();
            await _inventoryService.ReserveStockForOrderAsync(lines, order.Id, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return order;
        }
        catch (InsufficientStockException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<SalesOrder> TransitionStatusAsync(
        Guid orderId,
        OrderStatus targetStatus,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, includeItems: true)
            ?? throw new InvalidOperationException($"Pedido '{orderId}' não encontrado.");

        _stateMachine.EnsureCanTransition(order.Status, targetStatus);

        if (targetStatus == OrderStatus.Faturado)
        {
            throw new InvalidOperationException(
                "Use IInvoiceOrchestrator.PrepareInvoiceAsync para faturar o pedido.");
        }

        if (targetStatus == OrderStatus.Pago)
        {
            await _inventoryService.ConfirmStockDebitAsync(orderId, cancellationToken);
        }

        if (targetStatus == OrderStatus.Cancelado)
        {
            await _inventoryService.ReleaseStockReservationAsync(orderId, cancellationToken);
        }

        return await _orderRepository.UpdateStatusAsync(orderId, targetStatus);
    }
}

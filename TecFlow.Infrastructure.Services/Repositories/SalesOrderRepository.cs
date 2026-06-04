using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;

namespace TecFlow.Infrastructure.Services.Repositories;

public class SalesOrderRepository : ISalesOrderRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public SalesOrderRepository(AppDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<SalesOrder?> GetByIdAsync(Guid id, bool includeItems = false)
    {
        IQueryable<SalesOrder> query = _context.SalesOrders;

        if (includeItems)
        {
            query = query
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Include(o => o.Customer);
        }

        return await query.FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IReadOnlyList<SalesOrder>> ListAsync()
    {
        return await _context.SalesOrders
            .Include(o => o.Customer)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<string> GenerateNextOrderNumberAsync(Guid tenantId)
    {
        var prefix = DateTime.UtcNow.ToString("yyyyMM");
        var lastNumber = await _context.SalesOrders
            .IgnoreQueryFilters()
            .Where(o => o.TenantId == tenantId && o.OrderNumber.StartsWith($"PV-{prefix}-"))
            .OrderByDescending(o => o.OrderNumber)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync();

        var sequence = 1;
        if (!string.IsNullOrEmpty(lastNumber))
        {
            var parts = lastNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var parsed))
            {
                sequence = parsed + 1;
            }
        }

        return $"PV-{prefix}-{sequence:D6}";
    }

    public async Task<SalesOrder> CreateAsync(SalesOrder order)
    {
        order.CreatedAt = DateTime.UtcNow;
        await _context.SalesOrders.AddAsync(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<SalesOrder> UpdateAsync(SalesOrder order)
    {
        order.Touch();
        _context.SalesOrders.Update(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<SalesOrder> UpdateStatusAsync(Guid orderId, OrderStatus status)
    {
        var order = await GetByIdAsync(orderId, includeItems: true)
            ?? throw new InvalidOperationException($"Pedido '{orderId}' não encontrado.");

        order.Status = status;
        order.Touch();
        await _context.SaveChangesAsync();
        return order;
    }
}

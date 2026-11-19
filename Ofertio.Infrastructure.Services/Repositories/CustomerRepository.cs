using Microsoft.EntityFrameworkCore;
using Ofertio.Business.Interfaces.Repositories;
using Ofertio.Core.Entities;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Customer?> GetByIdAsync(int id) =>
        _context.Customers
            .Include(c => c.City).ThenInclude(city => city!.State)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IReadOnlyList<Customer>> ListAsync()
    {
        return await _context.Customers
            .Include(c => c.City).ThenInclude(city => city!.State)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        customer.CreatedAt = DateTime.UtcNow;
        await _context.Customers.AddAsync(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer> UpdateAsync(Customer customer)
    {
        customer.Touch();
        // Já rastreado (veio do GetByIdAsync com a cidade): o EF detecta as mudanças sozinho.
        // Update() marcaria também a cidade e o estado carregados como alterados.
        if (_context.Entry(customer).State == EntityState.Detached)
        {
            _context.Customers.Update(customer);
        }

        await _context.SaveChangesAsync();
        return customer;
    }
}

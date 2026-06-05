using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id);
    Task<IReadOnlyList<Customer>> ListAsync();
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
}

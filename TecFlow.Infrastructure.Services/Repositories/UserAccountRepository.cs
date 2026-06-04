using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Database;
using Microsoft.EntityFrameworkCore;

namespace TecFlow.Infrastructure.Services.Repositories
{
    public class UserAccountRepository : IUserAccountRepository
    {
        private readonly AppDbContext _context;
        private readonly ITenantProvisioningService _tenantProvisioning;

        public UserAccountRepository(AppDbContext context, ITenantProvisioningService tenantProvisioning)
        {
            _context = context;
            _tenantProvisioning = tenantProvisioning;
        }

        private async Task EnsureTenantAsync(UserAccount usuario)
        {
            if (usuario.TenantId == Guid.Empty)
            {
                await _tenantProvisioning.EnsureTenantForUserAsync(usuario);
            }
        }

        public async Task<UserAccount?> GetByIdAsync(int id)
        {
            return await _context.UserAccounts.FindAsync(id);
        }

        public async Task<IEnumerable<UserAccount>> GetAllAsync()
        {
            return await _context.UserAccounts.ToListAsync();
        }

        public async Task AddAsync(UserAccount usuario)
        {
            await EnsureTenantAsync(usuario);
            await _context.UserAccounts.AddAsync(usuario);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(UserAccount entity)
        {
            _context.UserAccounts.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.UserAccounts.FindAsync(id);
            if (entity != null)
            {
                _context.UserAccounts.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<UserAccount?> GetByIdIncludingOwnerAsync(int id)
        {
            // O EF Core lida com a consulta assíncrona ao banco de dados
            return await _context.UserAccounts.FirstOrDefaultAsync(c => c.Id == id);
        }


        public async Task<UserAccount> CriarAsync(UserAccount usuario)
        {
            // Geralmente, o CreatedAt e UpdatedAt são definidos pelo BD ou interceptor.
            // Se precisar definir aqui:
            usuario.CreatedAt = DateTime.UtcNow;
            usuario.UpdatedAt = DateTime.UtcNow;

            await EnsureTenantAsync(usuario);
            _context.UserAccounts.Add(usuario);
            await _context.SaveChangesAsync(); // Salva no BD e o EF Core popula o ID gerado.

            return usuario; // Retorna a entidade com o ID atribuído
        }

        public async Task<UserAccount?> GetByEmailAsync(string email)
        {
            return await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<UserAccount> CreateAsync(UserAccount usuario)
        {
            await EnsureTenantAsync(usuario);
            _context.UserAccounts.Add(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }
    }
}
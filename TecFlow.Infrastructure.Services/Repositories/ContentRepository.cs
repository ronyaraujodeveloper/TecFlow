using Microsoft.EntityFrameworkCore;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Infrastructure.Configuration;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Repositories
{
    public class ContentRepository : IContentRepository
    {
        private readonly AppDbContext _context;

        public ContentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Content?> GetByIdAsync(int id)
        {
            return await _context.Contents.FindAsync(id);
        }

        public async Task<IEnumerable<Content>> GetAllAsync()
        {
            return await _context.Contents.ToListAsync();

            // return await _context.Campaigns
            //.Where(c => c.OwnerId == userId)
            //.ToListAsync();
        }

        public async Task AddAsync(Content conteudo)
        {
            await _context.Contents.AddAsync(conteudo);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Content entity)
        {
            _context.Contents.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Contents.FindAsync(id);
            if (entity != null)
            {
                _context.Contents.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Content?> GetByIdIncludingOwnerAsync(int id)
        {
            // O EF Core lida com a consulta assíncrona ao banco de dados
            return await _context.Contents.FirstOrDefaultAsync(c => c.Id == id);
        }
        public async Task<Content> CriarAsync(Content conteudo)
        {
            // Geralmente, o CreatedAt e UpdatedAt são definidos pelo BD ou interceptor.
            // Se precisar definir aqui:
            conteudo.CreatedAt = DateTime.UtcNow;
            conteudo.UpdatedAt = DateTime.UtcNow;

            _context.Contents.Add(conteudo);
            await _context.SaveChangesAsync(); // Salva no BD e o EF Core popula o ID gerado.

            return conteudo; // Retorna a entidade com o ID atribuído
        }
        public async Task<IEnumerable<Content>> GetAllByOwnerAsync(int ownerId)
        {
            return await _context.Contents
                .Where(x => x.OwnerId == ownerId)
                .ToListAsync();
        }
        public async Task<IEnumerable<Content>> GetByOwnerIdAsync(int ownerId)
        {
            return await _context.Contents
                .Where(c => c.OwnerId == ownerId)
                .ToListAsync();
        }
        public async Task<Content> CreateAsync(Content conteudo)
        {
            _context.Contents.Add(conteudo);
            await _context.SaveChangesAsync();
            return conteudo;
        }
    }
}
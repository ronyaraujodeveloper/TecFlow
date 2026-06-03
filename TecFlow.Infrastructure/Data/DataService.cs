using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Database;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Infrastructure.Data
{
    public class DataService : IDataService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DataService> _logger; // Injetando logger para registrar operações

        public DataService(AppDbContext context, ILogger<DataService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GetByIdAsync<TEntity>(int id)
        public async Task<TEntity?> GetByIdAsync<TEntity>(int id) where TEntity : BaseEntity
        {
            return await _context.Set<TEntity>().FindAsync(new object[] { id });
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync<TEntity>() where TEntity : BaseEntity
        {
            return await _context.Set<TEntity>().ToListAsync();
        }

        /// <summary>
        /// Adiciona uma nova entidade ao contexto e salva as alterações no banco de dados.
        /// </summary>
        public async Task<TEntity> AddAsync<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            // Garante que CreatedAt seja definido se não for especificado.
            if (entity.CreatedAt == default) entity.CreatedAt = DateTime.UtcNow;

            try
            {
                await _context.Set<TEntity>().AddAsync(entity);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Entidade {EntityType} com ID {EntityId} adicionada com sucesso.", typeof(TEntity).Name, entity.Id);
                return entity; // Retorna a entidade, agora com o ID gerado pelo banco.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar entidade {EntityType} ao banco de dados.", typeof(TEntity).Name);
                throw;
            }
        }

        /// <summary>
        /// Atualiza uma entidade existente. O EF Core rastreia as mudanças.
        /// </summary>
        public async Task<TEntity> UpdateAsync<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (entity.Id == 0) throw new ArgumentException("A entidade a ser atualizada deve possuir um ID válido (maior que 0).");

            // Buscar a entidade existente para garantir que ela existe e para obter a instância rastreada pelo EF.
            var existing = await FindEntityAsync<TEntity>(entity.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Entidade do tipo {typeof(TEntity).Name} com Id {entity.Id} não encontrada para atualização.");
            }

            // Define a data de atualização.
            entity.Touch();

            try
            {
                // Marca a entidade como modificada. O EF detectará as alterações e gerará o SQL UPDATE apropriado.
                _context.Entry(entity).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Entidade {EntityType} com ID {EntityId} atualizada com sucesso.", typeof(TEntity).Name, entity.Id);
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar entidade {EntityType} com ID {EntityId}.", typeof(TEntity).Name, entity.Id);
                throw;
            }
        }

        /// <summary>
        /// Exclui uma entidade pelo seu ID. Exige implementação explícita da interface.
        /// </summary>
        public async Task DeleteAsync<TEntity>(int id) where TEntity : BaseEntity
        {
            var entityToDelete = await FindEntityAsync<TEntity>(id);
            if (entityToDelete == null)
            {
                throw new KeyNotFoundException($"Entidade do tipo {typeof(TEntity).Name} com Id {id} não encontrada para exclusão.");
            }

            try
            {
                _context.Set<TEntity>().Remove(entityToDelete);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Entidade {EntityType} com ID {EntityId} excluída com sucesso.", typeof(TEntity).Name, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir entidade {EntityType} com ID {EntityId}.", typeof(TEntity).Name, id);
                throw;
            }
        }

        /// <summary>
        /// Helper assíncrono para encontrar uma entidade pelo seu ID.
        /// Combina FindAsync (eficiente para PK) e FirstOrDefaultAsync (fallback).
        /// </summary>
        private async Task<TEntity?> FindEntityAsync<TEntity>(int id) where TEntity : BaseEntity
        {
            if (id == 0)
            {
                _logger.LogWarning("Tentativa de buscar entidade com ID 0.");
                return null; // IDs geralmente não são 0 em RDBMS
            }

            var dbSet = _context.Set<TEntity>();

            try
            {
                // FindAsync é otimizado para chaves primárias.
                var entity = await dbSet.FindAsync(id);

                // Se FindAsync não encontrar (pode acontecer em cenários mais complexos), tenta FirstOrDefaultAsync.
                if (entity == null)
                {
                    entity = await dbSet.FirstOrDefaultAsync(e => e.Id == id);
                }
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar entidade {EntityType} com ID {EntityId} no banco de dados.", typeof(TEntity).Name, id);
                throw; // Relança a exceção para o chamador lidar
            }
        }

    }
}
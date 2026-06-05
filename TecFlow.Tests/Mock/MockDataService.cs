// Arquivo: TecFlow.Tests.Mock\MockDataService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;

namespace TecFlow.Tests.Mock
{
    /// <summary>
    /// Implementação Mock de IDataService para testes unitários.
    /// Mantém os dados em memória e simula operações CRUD, com implementações explícitas para a interface.
    /// </summary>
    public class MockDataService : IDataService
    {
        // _mockDb armazena listas de entidades por tipo. 'object' é usado devido à natureza genérica,
        // mas é cuidadosamente convertido para o tipo correto.
        private readonly Dictionary<Type, object> _mockDb = new Dictionary<Type, object>();
        private int _nextId = 1; // Contador para a próxima ID a ser atribuída em cada coleção nova.

        public MockDataService()
        {
            InitializeMockDb();
        }

        /// <summary>
        /// Inicializa o banco de dados mock com algumas entidades de exemplo.
        /// </summary>
        private void InitializeMockDb()
        {
            // --- Mock de Produtos ---
            var produtos = new List<Product>
            {
                new Product { Id = 1, Name = "Laptop Gamer X", Features = "16GB RAM, RTX 4070", SalesVolume = 1500.50m, Rating = 4.8, CreatedAt = DateTime.UtcNow.AddDays(-5) },
                new Product { Id = 2, Name = "Teclado Mecânico RGB", Features = "Switches Cherry MX Red, Iluminação VR", SalesVolume = 800.25m, Rating = 4.5, CreatedAt = DateTime.UtcNow.AddDays(-3) }
            };
            _mockDb[typeof(Product)] = produtos;
            _nextId = Math.Max(_nextId, produtos.Max(p => p.Id) + 1); // Garante que _nextId seja sempre maior que o último ID.

            // --- Mock de Campanhas ---
            var campanhas = new List<Campaign>
            {
                new Campaign { Id = 1, Name = "Black Friday 2024", StartDate = new DateTime(2024, 11, 20), EndDate = new DateTime(2024, 11, 30), Budget = 5000.00m, CreatedAt = DateTime.UtcNow.AddDays(-10) },
                new Campaign { Id = 2, Name = "Natal 2024", StartDate = new DateTime(2024, 12, 1), EndDate = new DateTime(2024, 12, 25), Budget = 7500.00m, CreatedAt = DateTime.UtcNow.AddDays(-8) }
            };
            _mockDb[typeof(Campaign)] = campanhas;
            if (campanhas.Any() && campanhas.Max(c => c.Id) >= _nextId)
            {
                _nextId = campanhas.Max(c => c.Id) + 1;
            }

            // Adicione outras coleções de mock aqui se necessário (ex: Conteudo, Afiliado)
            // Exemplo:
            // _mockDb[typeof(Content)] = new List<Content>();
            // _mockDb[typeof(Affiliate)] = new List<Affiliate>();
        }

        // --- HELPER: Obter ou Criar Lista em Memória ---
        /// <summary>
        /// Obtém a lista em memória para um dado tipo de entidade. Se a lista não existir, cria uma nova.
        /// </summary>
        private List<TEntity> GetOrAddList<TEntity>() where TEntity : BaseEntity
        {
            var entityType = typeof(TEntity);
            if (!_mockDb.TryGetValue(entityType, out object? listObj))
            {
                listObj = new List<TEntity>();
                _mockDb[entityType] = listObj;
            }
            return (List<TEntity>)listObj;
        }

        // --- IMPLEMENTAÇÃO DOS MEMBROS DE IDataservice (MÉTODOS GENÉRICOS PÚBLICOS) ---
        // Estes são os métodos que serão chamados diretamente quando você usar seu MockDataService.
        // Eles são "públicos e genéricos" e não usam o prefixo IDataService.

        /// <summary>
        /// Adiciona uma nova entidade ao mock. Gera um ID se não existir.
        /// </summary>
        public Task<TEntity> AddAsync<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            // Gera um ID se a entidade não tiver um (mock behavior).
            if (entity.Id == 0)
            {
                entity.Id = _nextId++; // Atribui o próximo ID disponível globalmente para o mock.
            }
            // Define CreatedAt se não estiver definido.
            if (entity.CreatedAt == default) entity.CreatedAt = DateTime.UtcNow;

            var list = GetOrAddList<TEntity>();
            list.Add(entity); // Adiciona a entidade à lista mock.
            return Task.FromResult(entity); // Retorna a entidade adicionada.
        }

        /// <summary>
        /// Atualiza uma entidade no mock. Substitui a entidade antiga pela nova.
        /// </summary>
        public Task<TEntity> UpdateAsync<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var list = GetOrAddList<TEntity>();
            var existingEntity = list.FirstOrDefault(e => e.Id == entity.Id);

            if (existingEntity == null)
            {
                throw new KeyNotFoundException($"Mock: Entidade do tipo {typeof(TEntity).Name} com Id {entity.Id} não encontrada para atualização.");
            }

            // Simula a atualização de propriedades de entidade e UpdatedAt.
            entity.Touch();

            // Remove a antiga e adiciona a nova, simulando a atualização no mock.
            list.Remove(existingEntity);
            list.Add(entity);

            return Task.FromResult(entity); // Retorna a entidade atualizada.
        }

        // Nota: GetByIdAsync, GetAllAsync, DeleteAsync (e outros que podem ser necessários)
        // são implementados explicitamente para IDataService abaixo.


        // --- IMPLEMENTAÇÃO EXPLÍCITA DOS MEMBROS DE IDataservice ---
        // Estes métodos usam a sintaxe 'IDataService.NameMetodo' e são necessários
        // para garantir que o contrato da interface seja 100% cumprido.

        /// <summary>
        /// Obtém uma entidade pelo ID (Implementação Explícita para IDataService).
        /// </summary>
        public Task<TEntity?> GetByIdAsync<TEntity>(int id) where TEntity : BaseEntity
        {
            var list = GetOrAddList<TEntity>();
            var entity = list.FirstOrDefault(e => e.Id == id);
            return Task.FromResult(entity);
        }

        /// <summary>
        /// Obtém todas as entidades de um tipo (Implementação Explícita para IDataService).
        /// </summary>
        public Task<IEnumerable<TEntity>> GetAllAsync<TEntity>() where TEntity : BaseEntity
        {
            var list = GetOrAddList<TEntity>();
            return Task.FromResult(list.AsEnumerable());
        }

        /// <summary>
        /// Exclui uma entidade pelo ID (Implementação Explícita para IDataService).
        /// </summary>
        public Task DeleteAsync<TEntity>(int id) where TEntity : BaseEntity
        {
            var list = GetOrAddList<TEntity>();
            var entity = list.FirstOrDefault(e => e.Id == id);
            if (entity == null)
                throw new KeyNotFoundException($"Mock: Entidade {typeof(TEntity).Name} com Id {id} não encontrada.");

            list.Remove(entity);
            return Task.CompletedTask;
        }
    }
}
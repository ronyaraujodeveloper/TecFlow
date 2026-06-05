using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;
using TecFlow.Infrastructure.Data;
using TecFlow.Infrastructure.Services.Repositories;
using TecFlow.Util.Security;
using Xunit;

namespace TecFlow.Tests.Integration
{
    // Classe para agrupar testes de integração de dados
    public class ProductRepositoryIntegrationTests : IDisposable // Implementa IDisposable para limpar o contexto
    {
        private readonly AppDbContext _context;
        private readonly IProductRepository _productRepository;
        private readonly IDataService _dataService; // Se quiser testar o DataService junto

        public ProductRepositoryIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var encryptionService = new AesEncryptionService(
                "z4wTRplZYgexzdRDmuV59SFL6cYlJ8sJGtPWtrxmiko=");

            var currentTenant = new NullCurrentTenantService();
            _context = new AppDbContext(options, encryptionService, currentTenant);

            // Inicializa os repositórios e serviços que usam o DbContext
            _productRepository = new ProductRepository(_context, currentTenant);
            _dataService = new DataService(_context, NullLogger<DataService>.Instance);

            // Povoa o banco de dados com dados iniciais se necessário para certos testes
            SeedDatabase();
        }

        private void SeedDatabase()
        {
            _context.Products.AddRange(
                new Product { Id = 1, Name = "Produto Teste", SalesVolume = 100.0m, Rating = Convert.ToDouble(4.5m), CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new Product { Id = 2, Name = "Outro Produto", SalesVolume = 50.0m, Rating = Convert.ToDouble(3.0m), CreatedAt = DateTime.UtcNow.AddDays(-2) }
            );
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetByIdAsync_DeveRetornarProdutoExistente()
        {
            // Arrange - dados já seedados no construtor
            int idProduto = 1;

            // Act
            var produto = await _productRepository.GetByIdAsync(idProduto);

            // Assert
            Assert.NotNull(produto);
            Assert.Equal("Produto Teste", produto.Name);
            Assert.Equal(100.0m, produto.SalesVolume);
        }

        [Fact]
        public async Task GetByIdAsync_DeveRetornarNullParaIdInexistente()
        {
            // Arrange
            int idInexistente = 99;

            // Act
            var produto = await _productRepository.GetByIdAsync(idInexistente);

            // Assert
            Assert.Null(produto);
        }

        [Fact]
        public async Task GetAllAsync_DeveRetornarTodosOsProdutos()
        {
            // Arrange
            // Dados já seedados no construtor.

            // Act
            var produtos = await _productRepository.GetAllAsync();

            // Assert
            Assert.NotNull(produtos);
            Assert.Equal(2, produtos.Count());
        }

        [Fact]
        public async Task AddAsync_DeveAdicionarNovoProdutoAoContexto()
        {
            // Arrange
            var novoProduto = new Product { Name = "Novo Product de Teste", SalesVolume = 200.0m, Rating = Convert.ToDouble(4.8m), CreatedAt = DateTime.UtcNow };

            // Act
           await _productRepository.AddAsync(novoProduto);
            await _context.SaveChangesAsync();

            var produtoSalvo = await _productRepository.GetByIdAsync(novoProduto.Id);

            Assert.NotNull(produtoSalvo);
            Assert.True(produtoSalvo!.Id > 0);
            Assert.Equal("Novo Product de Teste", produtoSalvo.Name); Assert.Equal("Novo Product de Teste", produtoSalvo.Name);

            // Verifica se realmente foi adicionado ao contexto (usando métodos do DbContext)
            var produtoNoContext = _context.Products.Find(produtoSalvo.Id);
            Assert.NotNull(produtoNoContext);
            Assert.Equal(produtoSalvo.Name, produtoNoContext.Name);
        }

        [Fact]
        public async Task UpdateAsync_DeveAtualizarProdutoExistente()
        {
            // Arrange
            int idParaAtualizar = 1;
            var produtoExistente = await _productRepository.GetByIdAsync(idParaAtualizar);
            Assert.NotNull(produtoExistente); // Garantir que o produto exista antes de tentar atualizar

            string novoNome = "Produto Teste Atualizado";
            produtoExistente.Name = novoNome;
            produtoExistente.SalesVolume = 150.5m;

            // Act
            await _productRepository.UpdateAsync(produtoExistente);
            await _context.SaveChangesAsync();

            var produtoAtualizado = await _productRepository.GetByIdAsync(idParaAtualizar);

            // Assert
            Assert.NotNull(produtoAtualizado);
            Assert.Equal(idParaAtualizar, produtoAtualizado.Id);
            Assert.Equal(novoNome, produtoAtualizado.Name);
            Assert.Equal(150.5m, produtoAtualizado.SalesVolume);

            // Verifica se o registro no DB foi realmente atualizado
            var produtoNoContext = _context.Products.Find(idParaAtualizar);
            Assert.NotNull(produtoNoContext);
            Assert.Equal(novoNome, produtoNoContext.Name);
        }

        [Fact]
        public async Task DeleteAsync_DeveRemoverProdutoExistente()
        {
            // Arrange
            int idParaDeletar = 2; // Assumindo que o produto com ID 2 existe após o AddAsync

            // Act
            await _productRepository.DeleteAsync(idParaDeletar);
            await _context.SaveChangesAsync(); // Salvar as alterações

            // Assert
            var produtoNoContext = _context.Products.Find(idParaDeletar);
            Assert.Null(produtoNoContext); // Deve ser nulo após a exclusão
        }

        // --- Teste para o DataService usando os mesmos dados ---
        [Fact]
        public async Task DataService_GetByIdAsync_DeveRetornarProdutoCorreto()
        {
            // Arrange
            var produto = await _dataService.GetByIdAsync<Product>(1); // Usando DataService genérico

            // Assert
            Assert.NotNull(produto);
            Assert.Equal("Produto Teste", produto.Name);
        }

        [Fact]
        public async Task DataService_GetAllAsync_DeveRetornarTodosOsProdutos()
        {
            // Arrange
            var produtos = await _dataService.GetAllAsync<Product>();

            // Assert
            Assert.Equal(2, produtos.Count());
        }

        // Implementa o método Dispose para limpar o DbContext em memória após cada teste
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
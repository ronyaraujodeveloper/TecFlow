using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Repositories;

namespace TecFlow.Business.Service.Application
{
    public class PublicacaoApplicationService
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<PublicacaoApplicationService> _logger;

        public PublicacaoApplicationService(IProductRepository produtoRepository, ILogger<PublicacaoApplicationService> logger)
        {
            _productRepository = produtoRepository ?? throw new ArgumentNullException(nameof(produtoRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Gera publicações simuladas para todos os produtos
        public async Task<IEnumerable<string>> GerarPublicacoesAsync()
        {
            var produtos = await _productRepository.GetAllAsync();
            var publicacoes = produtos
                .Select(p => $"Publicação simulada para o produto '{p.Name}'")
                .ToList();

            return publicacoes;
        }

        // Publica conteúdo para um produto específico (simulado)
        public async Task<string> PublicarAsync(int produtoId)
        {
            var produto = await _productRepository.GetByIdAsync(produtoId);
            if (produto == null)
            {
                return $"Produto com Id {produtoId} não encontrado.";
            }

            _logger.LogInformation("Publicando conteúdo para o produto {Nome} (simulado).", produto.Name);
            await Task.Delay(10); // simulação
            return $"Publicação simulada publicada para '{produto.Name}'.";
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Business.Pipelines
{
    public class PublicacaoPipeline
    {
        private readonly ITikTokShopApi _tikTokApi;
        private readonly IShopeeApi _shopeeApi;
        private readonly IMetricRepository _metricRepository;
        private readonly IScoreService _scoreService;
        private readonly ILogger<PublicacaoPipeline> _logger;

        public PublicacaoPipeline(
            ITikTokShopApi tikTokApi,
            IShopeeApi shopeeApi,
            IMetricRepository metricaRepo,
            IScoreService scoreService,
            ILogger<PublicacaoPipeline> logger)
        {
            _tikTokApi = tikTokApi;
            _shopeeApi = shopeeApi;
            _metricRepository = metricaRepo;
            _scoreService = scoreService;
            _logger = logger;
        }

        public async Task PublicarProdutoComScoreAsync(Product produto)
        {
            var sucessoTikTok = await _tikTokApi.PublicarAnuncioAsync(produto);
            if (sucessoTikTok)
            {
                _logger.LogInformation("Produto {Nome} publicado com sucesso.", produto.Name);
            }

            var metrica = await _metricRepository.GetByCampaignIdAsync(produto.Id);
            var score = _scoreService.CalcularScoreViral(produto, metrica.FirstOrDefault() ?? new Metric());

            if (score > 70)
            {
                _logger.LogInformation(
                    "Produto {Nome} tem Score Viral {Score}. Publicando...",
                    produto.Name,
                    score);
                await _tikTokApi.PublicarAnuncioAsync(produto);
            }
            else
            {
                _logger.LogInformation(
                    "Produto {Nome} com Score {Score} baixo. Pulando publicaÃ§Ã£o.",
                    produto.Name,
                    score);
            }
        }

        public async Task PublicarEmPlataformasAsync(IReadOnlyList<Product> produtos)
        {
            var produtoParaPublicacaoTikTok = produtos.FirstOrDefault();
            if (produtoParaPublicacaoTikTok != null)
            {
                await _tikTokApi.PublicarAnuncioAsync(produtoParaPublicacaoTikTok);
                _logger.LogInformation(
                    "Publicado no TikTok Shop para o produto: {ProdutoNome}",
                    produtoParaPublicacaoTikTok.Name);
            }
            else
            {
                _logger.LogWarning("PublicaÃ§Ã£o: nenhum produto disponÃ­vel para TikTok Shop.");
            }

            var produtoParaPublicacaoShopee = produtos.LastOrDefault();
            if (produtoParaPublicacaoShopee != null)
            {
                await _shopeeApi.PublicarAnuncioAsync(produtoParaPublicacaoShopee);
                _logger.LogInformation(
                    "Publicado na Shopee para o produto: {ProdutoNome}",
                    produtoParaPublicacaoShopee.Name);
            }
            else
            {
                _logger.LogWarning("PublicaÃ§Ã£o: nenhum produto disponÃ­vel para Shopee.");
            }
        }
    }
}

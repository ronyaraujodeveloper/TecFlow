using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Business.Pipelines
{
    public class GeracaoConteudoPipeline
    {
        private readonly IAIService _aiService;
        private readonly IDataService _dataService;
        private readonly ILogger<GeracaoConteudoPipeline> _logger;

        public GeracaoConteudoPipeline(
            IAIService aiService,
            IDataService dataService,
            ILogger<GeracaoConteudoPipeline> logger)
        {
            _aiService = aiService;
            _dataService = dataService;
            _logger = logger;
        }

        public async Task AtualizarDescricaoProdutoAsync(Product produto)
        {
            _logger.LogInformation("GeraÃ§Ã£o: processando produto {ProdutoNome}.", produto.Name);

            var descricao = await _aiService.GerarDescricaoProdutoAsync(produto, "Focado em conversÃ£o");
            produto.Description = descricao;
            await _dataService.UpdateAsync(produto);
        }

        public async Task GerarConteudoAdicionalAsync(Product? produto)
        {
            if (produto == null)
            {
                _logger.LogWarning("GeraÃ§Ã£o: nÃ£o foi possÃ­vel obter um produto para conteÃºdo adicional.");
                return;
            }

            var descricaoIA = await _aiService.GerarDescricaoProdutoAsync(
                produto, "Gerar descriÃ§Ã£o focada em SEO e benefÃ­cios.");
            _logger.LogInformation(
                "DescriÃ§Ã£o gerada pela IA para '{ProdutoNome}': {Descricao}",
                produto.Name,
                descricaoIA);

            var titulosGerados = await _aiService.GerarVariacoesTitulosAsync(
                produto, 5, "TÃ­tulos otimizados para e-commerce.");
            _logger.LogInformation(
                "'{ProdutoNome}': TÃ­tulos gerados pela IA: {Titulos}",
                produto.Name,
                string.Join(", ", titulosGerados ?? new List<string>()));
        }
    }
}

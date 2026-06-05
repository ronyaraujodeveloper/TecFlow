using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TecFlow.Core.Entities;
using TecFlow.Core.Interfaces.Repositories;
using TecFlow.Core.Interfaces.Services;
using TecFlow.Infrastructure.Interfaces;

namespace TecFlow.Infrastructure.Services.Service.ExternalServices
{
    public class OrquestradorService : IOrquestradorService
    {
        private readonly IAIService _aiService;
        private readonly IDataService _dataService;
        private readonly IAppConfiguration _appConfiguration;
        private readonly ITikTokShopApi _tikTokApi;
        private readonly IShopeeApi _shopeeApi;
        private readonly ILogger<OrquestradorService> _logger;
        private readonly IMetricRepository _metricRepository;
        private readonly IScoreService _scoreService;

        private readonly object _statusLock = new();
        private DateTimeOffset? _lastRunAt;
        private string _lastRunStatus = "Nunca executado";
        private string? _lastRunMessage;
        private string? _lastRunError;
        private bool _isRunning;

        public OrquestradorService(
            IAIService aiService,
            IDataService dataService,
            IMetricRepository metricaRepo,
            IAppConfiguration appConfiguration,
            ITikTokShopApi tikTokApi,
            IScoreService scoreService,
            IShopeeApi shopeeApi,
            ILogger<OrquestradorService> logger)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _metricRepository = metricaRepo ?? throw new ArgumentNullException(nameof(metricaRepo));
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            _tikTokApi = tikTokApi ?? throw new ArgumentNullException(nameof(tikTokApi));
            _scoreService = scoreService ?? throw new ArgumentNullException(nameof(scoreService));
            _shopeeApi = shopeeApi ?? throw new ArgumentNullException(nameof(shopeeApi));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<string> GetExecutionStatusAsync()
        {
            lock (_statusLock)
            {
                return Task.FromResult($"Status: {_lastRunStatus}; Ultima execução: {_lastRunAt:u}; Mensagem: {_lastRunMessage ?? "-"}; Erro: {_lastRunError ?? "-"}; Em execução: {_isRunning}");
            }
        }

        public Task ExecuteOrchestrationAsync()
        {
            return ExecuteFullPipelineAsync();
        }

        public async Task ExecuteFullPipelineAsync()
        {
            lock (_statusLock)
            {
                _isRunning = true;
                _lastRunAt = DateTimeOffset.UtcNow;
                _lastRunStatus = "Executando";
                _lastRunMessage = null;
                _lastRunError = null;
            }

            try
            {
                _logger.LogInformation("Orquestrador: Iniciando execução do pipeline completo...");

                var produtos = await _dataService.GetAllAsync<Product>();
                if (produtos == null || !produtos.Any())
                {
                    _logger.LogWarning("Orquestrador: Nenhum produto encontrado para processamento no pipeline completo.");
                    AtualizarStatus("Concluído", "Nenhum produto encontrado.", null);
                    return;
                }
                int count = 0;

                foreach (var produto in produtos)
                {
                    _logger.LogInformation("Processando produto: {ProdutoNome}", produto.Name);

                    var descricao = await _aiService.GerarDescricaoProdutoAsync(produto, "Focado em conversão");
                    produto.Description = descricao;
                    await _dataService.UpdateAsync(produto);

                    var sucessoTikTok = await _tikTokApi.PublicarAnuncioAsync(produto);
                    if (sucessoTikTok)
                    {
                        _logger.LogInformation("Produto {Nome} publicado com sucesso.", produto.Name);
                    }

                    var metrica = await _metricRepository.GetByCampaignIdAsync(produto.Id);
                    var score = _scoreService.CalcularScoreViral(produto, metrica.FirstOrDefault() ?? new Metric());

                    if (score > 70)
                    {
                        _logger.LogInformation("Produto {Nome} tem Score Viral {Score}. Publicando...", produto.Name, score);
                        await _tikTokApi.PublicarAnuncioAsync(produto);
                    }
                    else
                    {
                        _logger.LogInformation("Produto {Nome} com Score {Score} baixo. Pulando publicação.", produto.Name, score);
                    }
                    count++;
                }

                var produtoParaIA = produtos.FirstOrDefault();
                string? descricaoIA = null;
                if (produtoParaIA != null)
                {
                    descricaoIA = await _aiService.GerarDescricaoProdutoAsync(produtoParaIA, "Gerar descrição focada em SEO e benefícios.");
                    _logger.LogInformation("Descrição gerada pela IA para '{ProdutoNome}': {Descricao}", produtoParaIA.Name, descricaoIA);
                }
                else
                {
                    _logger.LogWarning("Orquestrador: Não foi possível obter um produto para gerar descrição com IA.");
                }

                List<string>? titulosGerados = null;
                if (produtoParaIA != null)
                {
                    titulosGerados = await _aiService.GerarVariacoesTitulosAsync(produtoParaIA, 5, "Títulos otimizados para e-commerce.");
                    _logger.LogInformation("'{ProdutoNome}': Títulos gerados pela IA: {Titulos}", produtoParaIA.Name, string.Join(", ", titulosGerados ?? new List<string>()));
                }
                else
                {
                    _logger.LogWarning("Orquestrador: Não foi possível obter um produto para gerar variações de títulos.");
                }

                var produtoParaPublicacaoTikTok = produtos.FirstOrDefault();
                if (produtoParaPublicacaoTikTok != null)
                {
                    await _tikTokApi.PublicarAnuncioAsync(produtoParaPublicacaoTikTok);
                    _logger.LogInformation("Publicado no TikTok Shop para o produto: {ProdutoNome}", produtoParaPublicacaoTikTok.Name);
                }
                else
                {
                    _logger.LogWarning("Orquestrador: Nenhum produto disponível para publicar no TikTok Shop.");
                }

                var produtoParaPublicacaoShopee = produtos.LastOrDefault();
                if (produtoParaPublicacaoShopee != null)
                {
                    await _shopeeApi.PublicarAnuncioAsync(produtoParaPublicacaoShopee);
                    _logger.LogInformation("Publicado na Shopee para o produto: {ProdutoNome}", produtoParaPublicacaoShopee.Name);
                }
                else
                {
                    _logger.LogWarning("Orquestrador: Nenhum produto disponível para publicar na Shopee.");
                }
                AtualizarStatus("Concluído", $"Pipeline executado para {count} produtos.", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orquestrador: Falha ao executar o pipeline completo.");
                AtualizarStatus("Falha", "Erro durante a execução do pipeline.", ex.Message);
                throw;
            }
            finally
            {
                lock (_statusLock)
                {
                    _isRunning = false;
                }
            }
        }

        private void AtualizarStatus(string status, string message, string? error)
        {
            lock (_statusLock)
            {
                _lastRunStatus = status;
                _lastRunMessage = message;
                _lastRunError = error;
            }
        }
    }
}
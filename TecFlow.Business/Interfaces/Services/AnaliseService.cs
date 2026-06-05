// TecFlow.Core.Services\AnaliseService.cs
// Nenhuma alteração é necessária neste arquivo.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Repositories; // IDataService

namespace TecFlow.Business.Interfaces.Services // Assumindo que este serviço esteja em TecFlow.Core.Services
{
    /// <summary>
    /// Serviço responsável por orquestrar análises diversas, utilizando serviços de IA e de acesso a dados.
    /// </summary>
    public class AnaliseService : IAnaliseService // Assumindo que IAnaliseService também existe
    {
        private readonly IDataService _dataService;
        private readonly IAIService _aiService;
        private readonly IGeminiService _geminiService;
        private readonly ILogger<AnaliseService> _logger;

        public AnaliseService(
            IDataService dataService,
            IAIService aiService,
            IGeminiService geminiService,
            ILogger<AnaliseService> logger)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Analisa um texto fornecido chamando diferentes provedores de IA (IAIService, IGeminiService).
        /// </summary>
        public async Task<string> AnalisarTextoAsync(string texto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                _logger.LogWarning("Tentativa de analisar texto vazio.");
                return "Nenhum texto fornecido para análise.";
            }

            try
            {
                _logger.LogInformation("Analisando texto com provedores de IA...");

                var taskOpenAI = _aiService.GerarResumoAsync($"Resuma o seguinte texto de forma concisa:\n\n{texto}", cancellationToken);
                var taskGemini = _geminiService.SummarizeTextAsync(texto, cancellationToken);

                await Task.WhenAll(taskOpenAI, taskGemini);

                var resumoOpenAI = await taskOpenAI;
                var resumoGemini = await taskGemini;

                return $"Resumo (IA Service): {resumoOpenAI}\nResumo (Gemini Service): {resumoGemini}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao analisar texto com IA.");
                throw;
            }
        }

        /// <summary>
        /// Calcula uma pontuação de relevância para um item (Product).
        /// </summary>
        public async Task<decimal> CalcularPontuacaoDeRelevanciaAsync(int itemId, CancellationToken cancellationToken = default)
        {
            if (itemId <= 0)
            {
                _logger.LogWarning("Tentativa de calcular relevância com ID de item inválido: {ItemId}", itemId);
                return 0m;
            }

            try
            {
                _logger.LogInformation("Calculando pontuação de relevância para item (Product) ID: {ItemId}", itemId);

                // A chamada agora funcionará porque Product herda de BaseEntity
                var produto = await _dataService.GetByIdAsync<Product>(itemId);

                if (produto == null)
                {
                    _logger.LogWarning("Produto com ID {ItemId} não encontrado ao calcular pontuação de relevância.", itemId);
                    return 0m;
                }

                // --- LÓGICA DE CÁLCULO DE RELEVÂNCIA ---
                // Adapte esta lógica de acordo com seus requisitos de negócio.
                // As propriedades SalesVolume e Rating foram adicionadas à entidade Product para este exemplo.
                decimal score = 0m;

                score += produto.SalesVolume * 0.01m;
                score += (decimal)produto.Rating * 2.0m;

                _logger.LogInformation("Pontuação de relevância calculada para Product ID {ItemId}: {Score}. Detalhes: SalesVolume={SalesVolume}, Rating={Rating}",
                    itemId, score, produto.SalesVolume, produto.Rating);

                return score;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular pontuação de relevância para item (Product) ID {ItemId}.", itemId);
                throw;
            }
        }
    }
    // Assumindo que IAnaliseService e IDataService são interfaces válidas em seus namespaces.
}
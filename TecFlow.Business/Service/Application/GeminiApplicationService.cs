using Microsoft.Extensions.Logging;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Business.Service.Application
{
    public class GeminiApplicationService
    {
        private readonly IGeminiService _geminiService;
        private readonly ILogger<GeminiApplicationService> _logger;

        public GeminiApplicationService(IGeminiService geminiService, ILogger<GeminiApplicationService> logger)
        {
            _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Métodos que chamam os métodos genéricos da IGeminiService (com prompt string)
        public Task<string> GenerateDescriptionAsync(string prompt, CancellationToken cancellationToken = default)
            => _geminiService.GenerateDescriptionAsync(prompt, cancellationToken);

        public Task<string> GenerateScriptAsync(string prompt, CancellationToken cancellationToken = default)
            => _geminiService.GenerateScriptAsync(prompt, cancellationToken);

        public Task<string> GenerateTitleAsync(string prompt, CancellationToken cancellationToken = default)
            => _geminiService.GenerateTitleAsync(prompt, cancellationToken);

        public Task<string> SummarizeTextAsync(string text, CancellationToken cancellationToken = default)
            => _geminiService.SummarizeTextAsync(text, cancellationToken);

        // Métodos que chamam os métodos específicos da IGeminiService (com objeto Produto)
        public Task<string> GerarDescricaoProdutoAsync(Product produto, string contextoAdicional = "")
            => _geminiService.GerarDescricaoProdutoAsync(produto, contextoAdicional);

        public Task<List<string>> GerarVariacoesTitulosAsync(Product produto, int quantidade = 5, string contexto = "")
            => _geminiService.GerarVariacoesTitulosAsync(produto, quantidade, contexto);

        public Task<string> GerarScriptVideoAsync(Product produto, string formato, int duracaoEstimadaSegundos)
            => _geminiService.GerarScriptVideoAsync(produto, formato, duracaoEstimadaSegundos);

        public Task<string> GerarPromptCriativoVisualAsync(string descricaoConteudo, string estiloVisual, string plataformaAlvo)
            => _geminiService.GerarPromptCriativoVisualAsync(descricaoConteudo, estiloVisual, plataformaAlvo);

        public Task<string> GerarResumoAsync(string text)
            => _geminiService.GerarResumoAsync(text);

        public Task<string> GerarResumoAsync(string prompt, CancellationToken cancellationToken = default)
            => _geminiService.GerarResumoAsync(prompt, cancellationToken);
    }
}
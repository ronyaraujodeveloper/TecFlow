using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Business.Service.Application
{
    // Observação: o nome da classe segue o padrão Application Service,
    // mantendo a consistência com os nomes no seu código.
    public class AIApplicationService
    {
        private readonly IAIService _aiService;
        private readonly ILogger<AIApplicationService> _logger;

        public AIApplicationService(IAIService aiService, ILogger<AIApplicationService> logger)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<string> GerarDescricaoProdutoAsync(Product produto, string contextoAdicional = "")
            => _aiService.GerarDescricaoProdutoAsync(produto, contextoAdicional);

        public Task<List<string>> GerarVariacoesTitulosAsync(Product produto, int quantidade = 5, string contexto = "")
            => _aiService.GerarVariacoesTitulosAsync(produto, quantidade, contexto);

        public Task<string> GerarScriptVideoAsync(Product produto, string formato, int duracaoEstimadaSegundos)
            => _aiService.GerarScriptVideoAsync(produto, formato, duracaoEstimadaSegundos);

        public Task<string> GerarPromptCriativoVisualAsync(string descricaoConteudo, string estiloVisual, string plataformaAlvo)
            => _aiService.GerarPromptCriativoVisualAsync(descricaoConteudo, estiloVisual, plataformaAlvo);

        public Task<string> GerarResumoAsync(string texto)
            => _aiService.GerarResumoAsync(texto);

        public Task<string> GerarResumoAsync(string texto, CancellationToken cancellationToken)
            => _aiService.GerarResumoAsync(texto, cancellationToken);
    }
}
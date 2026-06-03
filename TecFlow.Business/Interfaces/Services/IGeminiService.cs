// TecFlow.Core\Interfaces\Services\IGeminiService.cs

using TecFlow.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TecFlow.Business.Interfaces.Services
{
    /// <summary>
    /// Contrato para serviços que interagem com a API do Google Gemini.
    /// </summary>
    public interface IGeminiService
    {
        // Métodos Genéricos (aceitam string prompt diretamente)
        Task<string> GenerateDescriptionAsync(string prompt, CancellationToken cancellationToken = default);
        Task<string> GenerateScriptAsync(string prompt, CancellationToken cancellationToken = default);
        Task<string> GenerateTitleAsync(string prompt, CancellationToken cancellationToken = default);
        Task<string> SummarizeTextAsync(string text, CancellationToken cancellationToken = default);

        // Métodos Específicos (aceitam entidade Produto)
        Task<string> GerarDescricaoProdutoAsync(Product produto, string contextoAdicional = "");
        Task<List<string>> GerarVariacoesTitulosAsync(Product produto, int quantidade = 5, string contexto = "");
        Task<string> GerarScriptVideoAsync(Product produto, string formato, int duracaoEstimadaSegundos);
        Task<string> GerarPromptCriativoVisualAsync(string descricaoConteudo, string estiloVisual, string plataformaAlvo);

        // Métodos de Resumo com variações
        Task<string> GerarResumoAsync(string text); // Versão simples
        Task<string> GerarResumoAsync(string prompt, CancellationToken cancellationToken = default); // Versão com prompt customizado
    }
}
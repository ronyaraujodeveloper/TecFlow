// Em TecFlow.Core.Interfaces/IAIService.cs
using System.Threading.Tasks;
using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Services
{
    public interface IAIService
    {
        // Assinatura correta com Async, parâmetros e retorno
        Task<string> GerarDescricaoProdutoAsync(Product produto, string contextoAdicional = "");
        Task<string> GerarScriptVideoAsync(Product produto, string formato, int duracaoEstimadaSegundos);
        Task<string> GerarPromptCriativoVisualAsync(string descricaoConteudo, string estiloVisual, string plataformaAlvo);

        Task<string> GerarResumoAsync(string text);
        Task<string> GerarResumoAsync(string prompt, CancellationToken cancellationToken = default);
        Task<List<string>> GerarVariacoesTitulosAsync(Product produto, int quantidade = 5, string contexto = "");
        
    }
}
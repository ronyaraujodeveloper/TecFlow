using System.Threading; // Certifique-se de ter este using
using System.Threading.Tasks;

namespace TecFlow.Business.Interfaces.Services
{
    public interface IAnaliseService
    {
        /// <summary>
        /// Analisa um texto fornecido e obtém resumos de diferentes provedores de IA.
        /// </summary>
        /// <param name="texto">O texto a ser analisado.</param>
        /// <param name="cancellationToken">Token para cancelamento da operação.</param>
        /// <returns>Uma string contendo os resumos gerados.</returns>
        Task<string> AnalisarTextoAsync(string texto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calcula a pontuação de relevância para um item.
        /// </summary>
        /// <param name="itemId">O ID do item a ser analisado.</param>
        /// <param name="cancellationToken">Token para cancelamento da operação.</param>
        /// <returns>A pontuação de relevância calculada.</returns>
        Task<decimal> CalcularPontuacaoDeRelevanciaAsync(int itemId, CancellationToken cancellationToken = default);

        // Outros métodos de análise específicos, se existirem
    }
}
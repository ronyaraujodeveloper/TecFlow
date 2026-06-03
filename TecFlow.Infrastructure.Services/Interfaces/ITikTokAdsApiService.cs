// Caminho Completo: TecFlow.Core\Interfaces\Services\ITikTokAdsApiService.cs

using System.Collections.Generic;
using System.Threading.Tasks;
using TecFlow.Core.Entities; // Supondo que Product ou outras entidades possam ser usadas

namespace TecFlow.Core.Interfaces.Services
{
    /// <summary>
    /// Contrato para interagir com a API de TikTok Ads.
    /// </summary>
    public interface ITikTokAdsApiService
    {
        /// <summary>
        /// Busca dados relevantes na API de TikTok Ads.
        /// O tipo de retorno 'object' é genérico e deve ser substituído por um tipo concreto
        /// que represente campanhas, audiências, anúncios ou outros recursos de ads.
        /// </summary>
        /// <param name="query">O termo de busca ou filtro.</param>
        /// <returns>Uma lista de resultados genéricos da busca.</returns>
        Task<List<object>> SearchAdsAsync(string query);

        /// <summary>
        /// Publica ou gerencia um anúncio na plataforma TikTok Ads.
        /// O tipo 'object' para o parâmetro 'anuncio' é genérico e deve ser substituído
        /// por um modelo de dados que represente a estrutura de um anúncio no TikTok Ads.
        /// </summary>
        /// <param name="anuncio">Os dados do anúncio a serem publicados.</param>
        /// <returns>True se a publicação foi bem-sucedida, false caso contrário.</returns>
        Task<bool> PublishAdAsync(object anuncio);

        // Adicione outros métodos conforme necessário, como para gerenciar campanhas, obter relatórios, etc.
    }
}
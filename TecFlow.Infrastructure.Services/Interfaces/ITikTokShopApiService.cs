// Caminho Completo: TecFlow.Core\Interfaces\Services\ITikTokShopApiService.cs

using System.Collections.Generic;
using System.Threading.Tasks;
using TecFlow.Core.Entities; // Necessário para a entidade Produto

namespace TecFlow.Core.Interfaces.Services
{
    /// <summary>
    /// Contrato para serviços que interagem com a API do TikTok Shop.
    /// </summary>
    public interface ITikTokShopApiService
    {
        /// <summary>
        /// Busca produtos no TikTok Shop com base em uma consulta.
        /// </summary>
        /// <param name="query">O termo de busca (ex: nome do produto, palavra-chave).</param>
        /// <returns>Uma lista de produtos encontrados no TikTok Shop.</returns>
        Task<List<Product>> SearchProductsAsync(string query);

        /// <summary>
        /// Publica uma listagem de produto no TikTok Shop.
        /// </summary>
        /// <param name="produto">O objeto Product do sistema TecFlow a ser publicado.</param>
        /// <returns>True se a publicação for bem-sucedida, false caso contrário.</returns>
        Task<bool> PublishProductListingAsync(Product produto);

        /// <summary>
        /// Gera um link de afiliado para um produto específico no TikTok Shop.
        /// </summary>
        /// <param name="produtoId">O ID do produto no TikTok Shop para o qual gerar o link.</param>
        /// <returns>O URL do link de afiliado gerado, ou null se a operação falhar.</returns>
        Task<string?> GenerateAffiliateLinkAsync(int produtoId);

        // Adicione outros métodos se a API do TikTok Shop expuser funcionalidades adicionais que você quer abstrair.
    }
}
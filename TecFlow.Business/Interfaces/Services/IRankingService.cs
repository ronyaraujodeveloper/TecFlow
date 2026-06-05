using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Services
{
    public interface IRankingService
    {
        Task<IEnumerable<RankedItem>> RankItemsAsync(IEnumerable<int> itemIds, CancellationToken cancellationToken = default);
        Task<IEnumerable<RankedItem>> RankItemsByPopularityAsync(IEnumerable<int> itemIds, CancellationToken cancellationToken = default);
    }

    // Exemplo de classe de domínio para retornar itens rankeados
    
}
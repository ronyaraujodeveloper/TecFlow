using TecFlow.Core.Enums;
using TecFlow.Database.Entity;

namespace TecFlow.Business.Interfaces.Repositories;

public interface IIntegracaoLojaRepository
{
    Task<IReadOnlyList<IntegracaoLoja>> ListByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<IntegracaoLoja?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IntegracaoLoja?> GetByUserShopPlatformAsync(
        int userId,
        string shopId,
        MarketplaceType platformType,
        CancellationToken cancellationToken = default);

    Task AddAsync(IntegracaoLoja entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(IntegracaoLoja entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

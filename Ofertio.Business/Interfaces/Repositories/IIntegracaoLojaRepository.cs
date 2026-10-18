using Ofertio.Core.Enums;
using Ofertio.Database.Entity;

namespace Ofertio.Business.Interfaces.Repositories;

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

using Ofertio.Business.Dto;
using Ofertio.Database.Filter;

namespace Ofertio.Business.Interfaces.Services;

public interface IIntegracaoLojaService
{
    Task<IntegracaoLojaResponseDto> ListByUserAsync(int userId, IntegracaoLojaFilter filter, CancellationToken cancellationToken = default);

    Task<IntegracaoLojaResponseDto> LinkAsync(int userId, IntegracaoLojaDto dto, CancellationToken cancellationToken = default);

    /// <summary>Atualiza apelido e ID de afiliado da loja existente sem alterar ShopId, plataforma ou tokens.</summary>
    Task<IntegracaoLojaResponseDto> UpdateAsync(int userId, int integrationId, IntegracaoLojaDto dto, CancellationToken cancellationToken = default);

    Task<IntegracaoLojaResponseDto> UnlinkAsync(int userId, int integrationId, CancellationToken cancellationToken = default);

    Task<ExpandAffiliateUrlResponseDto> ExpandAffiliateUrlAsync(
        string url,
        string platform,
        CancellationToken cancellationToken = default);
}

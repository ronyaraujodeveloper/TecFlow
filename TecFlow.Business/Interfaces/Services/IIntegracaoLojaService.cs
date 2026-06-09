using TecFlow.Business.Dto;
using TecFlow.Database.Filter;

namespace TecFlow.Business.Interfaces.Services;

public interface IIntegracaoLojaService
{
    Task<IntegracaoLojaResponseDto> ListByUserAsync(int userId, IntegracaoLojaFilter filter, CancellationToken cancellationToken = default);

    Task<IntegracaoLojaResponseDto> LinkAsync(int userId, IntegracaoLojaDto dto, CancellationToken cancellationToken = default);

    Task<IntegracaoLojaResponseDto> UnlinkAsync(int userId, int integrationId, CancellationToken cancellationToken = default);
}

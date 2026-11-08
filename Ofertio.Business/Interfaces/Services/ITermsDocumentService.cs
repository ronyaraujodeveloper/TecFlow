using Ofertio.Business.Dto;
using Ofertio.Core.Entities;
using Ofertio.Database.Filter;

namespace Ofertio.Business.Interfaces.Services;

public interface ITermsDocumentService
{
    Task<TermsVersionResponseDto> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<TermsVersionResponseDto> ListAsync(TermsVersionFilter filter, CancellationToken cancellationToken = default);

    Task<TermsVersionResponseDto> PublishAsync(
        TermsVersionDto request,
        CancellationToken cancellationToken = default);

    Task<UserTermsAcceptanceResponseDto> GetStatusAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserTermsAcceptanceResponseDto> AcceptAsync(
        int userId,
        UserTermsAcceptanceDto request,
        string? clientIp,
        CancellationToken cancellationToken = default);

    Task<UserTermsAcceptanceResponseDto> ListMineAsync(
        UserTermsAcceptanceFilter filter,
        CancellationToken cancellationToken = default);

    Task RecordRegistrationAsync(UserAccount user, string? clientIp, CancellationToken cancellationToken = default);
}

using Ofertio.Business.Dto.Auth;

namespace Ofertio.Business.Interfaces.Services;

public interface IPasswordResetService
{
    Task<AuthProviderResponseDto> RequestAsync(string? email, CancellationToken cancellationToken = default);

    Task<AuthProviderResponseDto> ConfirmAsync(string? token, string? newPassword, CancellationToken cancellationToken = default);
}

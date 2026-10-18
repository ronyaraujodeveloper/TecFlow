using Ofertio.Business.Dto;

namespace Ofertio.SharedUi.Services.Auth;

public interface IUserRegistrationApiService
{
    Task<UserResponseDto> RegisterAsync(UserDto request, CancellationToken cancellationToken = default);
}

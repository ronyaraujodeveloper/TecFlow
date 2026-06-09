using TecFlow.Business.Dto;

namespace TecFlow.SharedUi.Services.Auth;

public interface IUserRegistrationApiService
{
    Task<UserResponseDto> RegisterAsync(UserDto request, CancellationToken cancellationToken = default);
}

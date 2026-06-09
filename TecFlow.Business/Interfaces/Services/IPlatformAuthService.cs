using TecFlow.Business.Dto;
using TecFlow.Business.Dto.Auth;

namespace TecFlow.Business.Interfaces.Services;

public interface IPlatformAuthService
{
    Task<(bool Success, AuthTokenDto? Token, string? ErrorMessage, string? ErrorCode)> LoginAsync(
        string platform,
        PlatformAuthDto request,
        CancellationToken cancellationToken = default);

    Task<AuthProviderResponseDto> LinkProviderAsync(
        int userId,
        LinkProviderDto request,
        CancellationToken cancellationToken = default);

    Task<AuthProviderResponseDto> UnlinkProviderAsync(
        int userId,
        string provider,
        CancellationToken cancellationToken = default);

    Task<AuthProviderResponseDto> ChangePasswordAsync(
        int userId,
        ChangePasswordDto request,
        CancellationToken cancellationToken = default);

    Task<UserResponseDto> RegisterAsync(
        UserDto request,
        CancellationToken cancellationToken = default);
}

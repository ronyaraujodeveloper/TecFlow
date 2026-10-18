using Ofertio.Business.Dto;
using Ofertio.SharedUi.Models;

namespace Ofertio.SharedUi.Services.Devices;

public interface IDeviceRegistrationApiService
{
    Task<ApiResult<DeviceRegisterResponseDto>> RegisterDeviceAsync(
        string deviceToken,
        string platform,
        string? deviceId = null,
        CancellationToken cancellationToken = default);
}

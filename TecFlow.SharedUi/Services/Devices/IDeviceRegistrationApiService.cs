using TecFlow.Business.Dto;
using TecFlow.SharedUi.Models;

namespace TecFlow.SharedUi.Services.Devices;

public interface IDeviceRegistrationApiService
{
    Task<ApiResult<DeviceRegisterResponseDto>> RegisterDeviceAsync(
        string deviceToken,
        string platform,
        string? deviceId = null,
        CancellationToken cancellationToken = default);
}

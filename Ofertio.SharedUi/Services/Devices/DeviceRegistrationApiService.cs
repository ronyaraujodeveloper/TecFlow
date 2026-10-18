using Ofertio.Business.Dto;
using Ofertio.SharedUi.Models;
using Ofertio.SharedUi.Services.Http;

namespace Ofertio.SharedUi.Services.Devices;

public class DeviceRegistrationApiService : IDeviceRegistrationApiService
{
    private readonly IHttpService _httpService;

    public DeviceRegistrationApiService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public Task<ApiResult<DeviceRegisterResponseDto>> RegisterDeviceAsync(
        string deviceToken,
        string platform,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        var dto = new DeviceRegisterDto
        {
            DeviceToken = deviceToken,
            Platform = platform,
            DeviceId = deviceId
        };

        return _httpService.PostAsync<DeviceRegisterDto, DeviceRegisterResponseDto>(
            "api/devices/register",
            dto,
            cancellationToken);
    }
}

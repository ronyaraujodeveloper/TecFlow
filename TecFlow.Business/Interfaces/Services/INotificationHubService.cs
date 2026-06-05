using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Services;

public interface INotificationHubService
{
    Task<DeviceRegisterResponseDto> RegisterDeviceAsync(int ownerId, DeviceRegisterDto dto, CancellationToken cancellationToken = default);
    Task<NotificationSendResponseDto> SendToOwnerAsync(int ownerId, PushNotificationDto notification, CancellationToken cancellationToken = default);
    bool IsConfigured { get; }
}

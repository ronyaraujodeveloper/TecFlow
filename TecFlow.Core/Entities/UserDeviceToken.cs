namespace TecFlow.Core.Entities;

/// <summary>Token FCM/APNs do dispositivo móvel associado ao utilizador (OwnerId).</summary>
public class UserDeviceToken
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

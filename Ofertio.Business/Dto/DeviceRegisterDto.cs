namespace Ofertio.Business.Dto;

public class DeviceRegisterDto
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
}

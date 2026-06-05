namespace TecFlow.Business.Dto;

public class DeviceRegisterResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int? DeviceRegistrationId { get; set; }
}

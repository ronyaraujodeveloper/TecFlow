namespace TecFlow.Business.Dto;

public class WhatsAppIntegrationDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string InstanceName { get; set; } = string.Empty;

    public string ConnectionStatus { get; set; } = string.Empty;

    public string UiStatusLabel { get; set; } = "Desconectado";

    public string? PhoneNumber { get; set; }

    public string? ProfileName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastConnectedAt { get; set; }

    public bool IsActive { get; set; }

    public bool IsConnected { get; set; }

    public string? QrCodeDataUrl { get; set; }
}

public class EvolutionConnectionStateDto
{
    public string State { get; set; } = "close";

    public string? PhoneNumber { get; set; }

    public string? ProfileName { get; set; }
}

public class WhatsAppIntegrationResponseDto : ResponseDto
{
    public WhatsAppIntegrationDto? Data { get; set; }
}

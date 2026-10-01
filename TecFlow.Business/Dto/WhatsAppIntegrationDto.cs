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

    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastConnectedAt { get; set; }

    public bool IsActive { get; set; }

    public bool IsConnected { get; set; }

    public string? QrCodeDataUrl { get; set; }

    public bool EnableAutoConvertBot { get; set; } = true;

    public bool ReplyToPrivateMessages { get; set; } = true;

    public bool ReplyToGroupMessages { get; set; }

    public bool HasToken { get; set; }

    public string TokenMasked { get; set; } = string.Empty;

    public bool HasApiKey { get; set; }

    public string ApiKeyMasked { get; set; } = string.Empty;

    public bool HasSessionData { get; set; }

    public string SessionDataMasked { get; set; } = string.Empty;
}

public class WhatsAppBotPreferencesDto
{
    public bool EnableAutoConvertBot { get; set; } = true;

    public bool ReplyToPrivateMessages { get; set; } = true;

    public bool ReplyToGroupMessages { get; set; }
}

public class EvolutionConnectionStateDto
{
    public string State { get; set; } = "close";

    public string? PhoneNumber { get; set; }

    public string? ProfileName { get; set; }

    public string? ProfilePictureUrl { get; set; }
}

public class WhatsAppIntegrationResponseDto : ResponseDto
{
    public WhatsAppIntegrationDto? Data { get; set; }
}

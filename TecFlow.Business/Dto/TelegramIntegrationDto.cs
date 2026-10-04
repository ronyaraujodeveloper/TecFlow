namespace TecFlow.Business.Dto;

public class TelegramIntegrationDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? ChatId { get; set; }

    public string? BotUsername { get; set; }

    public bool IsActive { get; set; }

    public bool HasToken { get; set; }

    public string TokenMasked { get; set; } = string.Empty;

    public bool HasApiKey { get; set; }

    public string ApiKeyMasked { get; set; } = string.Empty;

    public bool HasSessionData { get; set; }

    public string SessionDataMasked { get; set; } = string.Empty;

    public int? UserBotApiId { get; set; }

    public bool HasUserBotApiHash { get; set; }

    public string UserBotApiHashMasked { get; set; } = string.Empty;

    public string? UserBotPhone { get; set; }

    public bool HasUserBotSession { get; set; }

    public bool UserBotAwaitingCode { get; set; }

    public string UserBotStatusLabel { get; set; } = "Inativo";

    public bool IsConnected { get; set; }

    public bool WebhookRegistered { get; set; }

    public string UiStatusLabel { get; set; } = "Desconectado";
}

public class SaveTelegramIntegrationDto
{
    public string? Token { get; set; }

    public string? ApiKey { get; set; }

    public string? ChatId { get; set; }

    public string? BotUsername { get; set; }

    public int? UserBotApiId { get; set; }

    public string? UserBotApiHash { get; set; }

    public string? UserBotPhone { get; set; }

    public string? UserBotVerificationCode { get; set; }
}

public class TelegramIntegrationResponseDto : ResponseDto
{
    public TelegramIntegrationDto? Data { get; set; }
}

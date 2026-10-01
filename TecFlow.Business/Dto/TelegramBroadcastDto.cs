namespace TecFlow.Business.Dto;

public class TelegramBroadcastCampaignDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string MessageText { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string TargetChatId { get; set; } = string.Empty;

    public DateTime ScheduledAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public string UiStatusLabel { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class TelegramScheduleCampaignDto
{
    public string Title { get; set; } = string.Empty;

    public string MessageText { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string? TargetChatId { get; set; }

    public DateTime? ScheduledAt { get; set; }
}

public class TelegramBroadcastResponseDto : ResponseDto
{
    public List<TelegramBroadcastCampaignDto> Campaigns { get; set; } = [];

    public TelegramBroadcastCampaignDto? Campaign { get; set; }

    public string? DefaultChatId { get; set; }
}

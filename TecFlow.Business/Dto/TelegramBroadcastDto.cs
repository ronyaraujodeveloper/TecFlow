namespace TecFlow.Business.Dto;

public class TelegramGroupDto
{
    public int Id { get; set; }

    public string ChatId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public int ParticipantCount { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsActive { get; set; }
}

public class TelegramBroadcastCampaignDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string MessageText { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string TargetChatId { get; set; } = string.Empty;

    public List<string> TargetChatIds { get; set; } = [];

    public List<string> TargetChatNames { get; set; } = [];

    public DateTime ScheduledAt { get; set; }

    public int IntervalSeconds { get; set; }

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

    public List<string> TargetChatIds { get; set; } = [];

    public List<string> SelectedChatIds { get; set; } = [];

    public DateTime? ScheduledAt { get; set; }

    public int IntervalSeconds { get; set; } = 30;
}

public class TelegramBroadcastResponseDto : ResponseDto
{
    public List<TelegramGroupDto> Groups { get; set; } = [];

    public List<TelegramBroadcastCampaignDto> Campaigns { get; set; } = [];

    public TelegramBroadcastCampaignDto? Campaign { get; set; }

    public string? DefaultChatId { get; set; }
}

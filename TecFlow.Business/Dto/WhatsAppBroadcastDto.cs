namespace TecFlow.Business.Dto;

public class EvolutionWhatsAppGroupDto
{
    public string Jid { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int ParticipantCount { get; set; }

    public bool IsAdmin { get; set; }
}

public class WhatsAppGroupDto
{
    public int Id { get; set; }

    public string Jid { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int ParticipantCount { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsActive { get; set; }
}

public class WhatsAppBroadcastCampaignDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string MessageText { get; set; } = string.Empty;

    public string? CommissionLinkUrl { get; set; }

    public string? ImageUrl { get; set; }

    public List<string> TargetGroupJids { get; set; } = [];

    public List<string> TargetGroupNames { get; set; } = [];

    public DateTime ScheduledAt { get; set; }

    public int IntervalSeconds { get; set; }

    public string Status { get; set; } = string.Empty;

    public string UiStatusLabel { get; set; } = string.Empty;

    public bool CanEdit { get; set; }

    public bool CanDelete { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class WhatsAppScheduleCampaignDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string MessageText { get; set; } = string.Empty;

    public string? CommissionLinkUrl { get; set; }

    public string? ImageUrl { get; set; }

    public List<string> TargetGroupJids { get; set; } = [];

    public DateTime? ScheduledAt { get; set; }

    public int IntervalSeconds { get; set; } = 30;
}

public class WhatsAppBroadcastResponseDto : ResponseDto
{
    public List<WhatsAppGroupDto> Groups { get; set; } = [];

    public List<WhatsAppBroadcastCampaignDto> Campaigns { get; set; } = [];

    public WhatsAppBroadcastCampaignDto? Campaign { get; set; }
}

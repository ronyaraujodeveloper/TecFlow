namespace TecFlow.Business.Dto;

public sealed class UpdateAgendamentoCommand
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

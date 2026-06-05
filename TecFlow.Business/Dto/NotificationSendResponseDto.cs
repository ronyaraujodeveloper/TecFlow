namespace TecFlow.Business.Dto;

public class NotificationSendResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
}

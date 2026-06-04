namespace TecFlow.Business.Dto;

public class PushNotificationDto
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? DeepLinkRoute { get; set; }
    public Dictionary<string, string> Data { get; set; } = new(StringComparer.Ordinal);
}

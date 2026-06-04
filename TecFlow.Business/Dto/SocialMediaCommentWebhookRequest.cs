using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class SocialMediaCommentWebhookRequest
{
    public string PostId { get; set; } = string.Empty;
    public SocialMediaType Platform { get; set; }
    public string Username { get; set; } = string.Empty;
    public string CommentText { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public int OwnerId { get; set; } = 1;
    public Dictionary<string, string>? Metadata { get; set; }
}

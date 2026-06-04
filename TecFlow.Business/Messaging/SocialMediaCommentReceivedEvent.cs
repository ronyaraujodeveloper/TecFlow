using TecFlow.Core.Enums;

namespace TecFlow.Business.Messaging;

/// <summary>Mensagem publicada na fila quando um comentário de rede social é recebido via webhook.</summary>
public class SocialMediaCommentReceivedEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PostId { get; set; } = string.Empty;
    public SocialMediaType Platform { get; set; }
    public string Username { get; set; } = string.Empty;
    public string CommentText { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public int OwnerId { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

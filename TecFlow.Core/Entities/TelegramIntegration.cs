using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

[Table("TelegramIntegrations")]
public class TelegramIntegration : BaseEntity
{
    public int UserId { get; set; }

    public string? Token { get; set; }

    public string? ApiKey { get; set; }

    public string? SessionData { get; set; }

    [MaxLength(64)]
    public string? ChatId { get; set; }

    [MaxLength(128)]
    public string? BotUsername { get; set; }

    public bool IsActive { get; set; } = true;
}

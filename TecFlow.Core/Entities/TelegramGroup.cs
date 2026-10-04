using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

[Table("TelegramGroups")]
public class TelegramGroup : BaseEntity
{
    public int UserId { get; set; }

    [Required]
    [MaxLength(64)]
    public string ChatId { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    public int ParticipantCount { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsActive { get; set; } = true;
}

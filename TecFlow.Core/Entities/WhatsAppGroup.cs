using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

[Table("WhatsAppGroups")]
public class WhatsAppGroup : BaseEntity
{
    public int UserId { get; set; }

    [Required]
    [MaxLength(128)]
    public string Jid { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    public int ParticipantCount { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsActive { get; set; } = true;
}

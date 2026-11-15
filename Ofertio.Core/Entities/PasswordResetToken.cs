using System.ComponentModel.DataAnnotations;

namespace Ofertio.Core.Entities;

/// <summary>Pedido de nova senha. O banco guarda só o hash do token.</summary>
public class PasswordResetToken : BaseEntity
{
    public int UserId { get; set; }

    public UserAccount? User { get; set; }

    [MaxLength(64)]
    public required string TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }
}

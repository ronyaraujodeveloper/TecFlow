using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ofertio.Core.Entities;

/// <summary>Registro de cada aceite (cadastro, reaceite ou primeiro vínculo de API externa).</summary>
[Table("UserTermsAcceptances")]
public class UserTermsAcceptance : BaseEntity
{
    public int UserId { get; set; }

    public UserAccount? User { get; set; }

    [MaxLength(16)]
    public string DocumentKind { get; set; } = "Terms";

    [MaxLength(16)]
    public string Version { get; set; } = string.Empty;

    public DateTime AcceptedAt { get; set; }

    [MaxLength(64)]
    public string? AcceptedIp { get; set; }

    /// <summary>Account ou ExternalApi.</summary>
    [MaxLength(16)]
    public string Purpose { get; set; } = "Account";
}

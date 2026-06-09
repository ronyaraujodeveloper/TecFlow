using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

/// <summary>
/// Vínculo de login externo (Google, Facebook, Apple) compatível com o esquema AspNetUserLogins do Identity.
/// Um usuário pode possuir múltiplos registros (1:N).
/// </summary>
[Table("AspNetUserLogins")]
public class UserExternalLogin
{
    [Required]
    [MaxLength(128)]
    public string LoginProvider { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ProviderKey { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? ProviderDisplayName { get; set; }

    public int UserId { get; set; }

    public UserAccount User { get; set; } = null!;
}

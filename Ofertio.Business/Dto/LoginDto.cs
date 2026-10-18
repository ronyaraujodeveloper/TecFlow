using System.ComponentModel.DataAnnotations;
using Ofertio.Util.Validation;

namespace Ofertio.Business.Dto;

public class LoginDto
{
    [Required]
    [ValidEmail(ErrorMessage = "E-mail inv�lido.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;
using TecFlow.Util.Validation;

namespace TecFlow.Business.Dto;

public class LoginDto
{
    [Required]
    [ValidEmail(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

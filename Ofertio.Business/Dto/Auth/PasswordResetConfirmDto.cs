namespace Ofertio.Business.Dto.Auth;

public class PasswordResetConfirmDto
{
    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}

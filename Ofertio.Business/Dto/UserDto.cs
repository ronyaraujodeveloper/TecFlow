namespace Ofertio.Business.Dto;

public class UserDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>O cadastro só grava a conta quando a pessoa marca o aceite. O IP não vem neste objeto.</summary>
    public bool AcceptedTerms { get; set; }
}

namespace TecFlow.Business.Dto;

public class UserAccountDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Plan { get; set; } = "Free";
    public string? WhatsAppPhone { get; set; }
}

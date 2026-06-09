namespace TecFlow.Business.Dto.Auth;

public class AuthProviderInfoDto
{
    public string Provider { get; set; } = string.Empty;
    public string? ProviderDisplayName { get; set; }
    public bool HasPassword { get; set; }
    public IReadOnlyList<string> LinkedProviders { get; set; } = Array.Empty<string>();
}

public class AuthProviderResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public AuthProviderInfoDto? Data { get; set; }
}

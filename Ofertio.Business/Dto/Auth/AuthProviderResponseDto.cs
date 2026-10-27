namespace Ofertio.Business.Dto.Auth;

public class AuthProviderInfoDto
{
    public string Provider { get; set; } = string.Empty;
    public string? ProviderDisplayName { get; set; }
    public bool HasPassword { get; set; }
    public IReadOnlyList<string> LinkedProviders { get; set; } = Array.Empty<string>();

    /// <summary>UF que define o fuso dos horários na tela; null = Brasília.</summary>
    public string? StateCode { get; set; }
}

public class ChangeStateDto
{
    /// <summary>UF (ex.: SP, AM). Vazio volta para Brasília.</summary>
    public string? StateCode { get; set; }
}

public class AuthProviderResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public AuthProviderInfoDto? Data { get; set; }
}

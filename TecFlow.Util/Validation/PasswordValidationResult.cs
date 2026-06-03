namespace TecFlow.Util.Validation;

/// <summary>
/// Resultado da validação de força de senha.
/// </summary>
public sealed class PasswordValidationResult
{
    public bool IsValid { get; init; }

    public List<string> Errors { get; init; } = [];
}

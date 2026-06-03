using System.ComponentModel.DataAnnotations;

namespace TecFlow.Util.Validation;

/// <summary>
/// Valida e-mail via <see cref="ValidationHelper.IsValidEmail"/> (DataAnnotations / Blazor forms).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ValidEmailAttribute : ValidationAttribute
{
    public ValidEmailAttribute()
        : base("E-mail inválido.")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string email || string.IsNullOrWhiteSpace(email))
        {
            return ValidationResult.Success;
        }

        return ValidationHelper.IsValidEmail(email)
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage);
    }
}

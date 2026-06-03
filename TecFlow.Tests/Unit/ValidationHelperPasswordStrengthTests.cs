using TecFlow.Util.Validation;

namespace TecFlow.Tests.Unit;

public class ValidationHelperPasswordStrengthTests
{
    [Fact]
    public void ValidatePasswordStrength_ReturnsInvalid_WhenPasswordIsNullOrEmpty()
    {
        var nullResult = ValidationHelper.ValidatePasswordStrength(null);
        var emptyResult = ValidationHelper.ValidatePasswordStrength(string.Empty);

        Assert.False(nullResult.IsValid);
        Assert.False(emptyResult.IsValid);
        Assert.Contains("A senha é obrigatória.", nullResult.Errors);
        Assert.Contains("A senha é obrigatória.", emptyResult.Errors);
    }

    [Fact]
    public void ValidatePasswordStrength_ReturnsValid_ForStrongPassword()
    {
        var result = ValidationHelper.ValidatePasswordStrength("Test@123");

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidatePasswordStrength_ReturnsAllMissingRules_WhenPasswordIsWeak()
    {
        var result = ValidationHelper.ValidatePasswordStrength("abc");

        Assert.False(result.IsValid);
        Assert.Equal(4, result.Errors.Count);
        Assert.Contains("A senha deve ter pelo menos 8 caracteres.", result.Errors);
        Assert.Contains("A senha deve conter pelo menos uma letra maiúscula.", result.Errors);
        Assert.Contains("A senha deve conter pelo menos um número.", result.Errors);
        Assert.Contains("A senha deve conter pelo menos um caractere especial.", result.Errors);
        Assert.DoesNotContain("A senha deve conter pelo menos uma letra minúscula.", result.Errors);
    }

    [Theory]
    [InlineData("test@1234", "A senha deve conter pelo menos uma letra maiúscula.")]
    [InlineData("TEST@1234", "A senha deve conter pelo menos uma letra minúscula.")]
    [InlineData("Test@abcd", "A senha deve conter pelo menos um número.")]
    [InlineData("Test1234", "A senha deve conter pelo menos um caractere especial.")]
    [InlineData("Te@1", "A senha deve ter pelo menos 8 caracteres.")]
    public void ValidatePasswordStrength_ReturnsSpecificError_WhenRuleFails(
        string password,
        string expectedError)
    {
        var result = ValidationHelper.ValidatePasswordStrength(password);

        Assert.False(result.IsValid);
        Assert.Contains(expectedError, result.Errors);
    }
}

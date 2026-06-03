using TecFlow.Util.Validation;

namespace TecFlow.Tests.Unit;

public class ValidationHelperTests
{
    [Theory]
    [InlineData("demo@TecFlow.local")]
    [InlineData("user@example.com")]
    [InlineData("User.Name+tag@sub.domain.co.uk")]
    public void IsValidEmail_ReturnsTrue_ForValidAddresses(string email)
    {
        Assert.True(ValidationHelper.IsValidEmail(email));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("@missing-local.com")]
    [InlineData("user@")]
    [InlineData("user@domain")]
    [InlineData(".user@domain.com")]
    [InlineData("user..name@domain.com")]
    public void IsValidEmail_ReturnsFalse_ForInvalidAddresses(string? email)
    {
        Assert.False(ValidationHelper.IsValidEmail(email));
    }

    [Fact]
    public void IsValidEmail_ReturnsFalse_WhenExceedsMaxLength()
    {
        var localPart = new string('a', 245);
        var email = $"{localPart}@example.com";

        Assert.False(ValidationHelper.IsValidEmail(email));
    }

    [Fact]
    public void IsValidEmail_TrimsWhitespace()
    {
        Assert.True(ValidationHelper.IsValidEmail("  demo@TecFlow.local  "));
    }

    [Theory]
    [InlineData("52998224725")]
    [InlineData("529.982.247-25")]
    public void IsValidCpf_ReturnsTrue_ForValidCpf(string cpf)
    {
        Assert.True(ValidationHelper.IsValidCpf(cpf));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("11111111111")]
    [InlineData("1234567890")]
    [InlineData("52998224724")]
    public void IsValidCpf_ReturnsFalse_ForInvalidCpf(string? cpf)
    {
        Assert.False(ValidationHelper.IsValidCpf(cpf));
    }

    [Theory]
    [InlineData("11222333000181")]
    [InlineData("11.222.333/0001-81")]
    public void IsValidCnpj_ReturnsTrue_ForLegacyNumericCnpj(string cnpj)
    {
        Assert.True(ValidationHelper.IsValidCnpj(cnpj));
    }

    [Fact]
    public void IsValidCnpj_ReturnsTrue_ForAlphanumericRootWithNumericSuffix()
    {
        Assert.True(ValidationHelper.IsValidCnpj("12ABC345000188"));
        Assert.True(ValidationHelper.IsValidCnpj("12.ABC.345/0001-88"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("11111111111111")]
    [InlineData("12ABC34501DE35")]
    [InlineData("12.ABC.345/01DE-35")]
    [InlineData("04525201100011")]
    public void IsValidCnpj_ReturnsFalse_ForInvalidCnpj(string? cnpj)
    {
        Assert.False(ValidationHelper.IsValidCnpj(cnpj));
    }
}

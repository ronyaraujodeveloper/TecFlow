using TecFlow.Util.Validation;

namespace TecFlow.Tests.Unit;

public class ValidationHelperPhoneTests
{
    [Theory]
    [InlineData("11987654321")]
    [InlineData("(11) 98765-4321")]
    [InlineData("+55 11 98765-4321")]
    [InlineData("55 11 98765-4321")]
    [InlineData("11 9 8765-4321")]
    public void IsValidBrazilianCellPhone_ReturnsTrue_ForValidNumbers(string phone)
    {
        Assert.True(ValidationHelper.IsValidBrazilianCellPhone(phone));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567890")]
    [InlineData("123456789012")]
    [InlineData("20987654321")]
    [InlineData("11887654321")]
    [InlineData("11907654321")]
    [InlineData("11900000000")]
    [InlineData("(11) 8765-4321")]
    public void IsValidBrazilianCellPhone_ReturnsFalse_ForInvalidNumbers(string? phone)
    {
        Assert.False(ValidationHelper.IsValidBrazilianCellPhone(phone));
    }

    [Theory]
    [InlineData("20987654321")]
    [InlineData("30987654321")]
    [InlineData("40987654321")]
    public void IsValidBrazilianCellPhone_ReturnsFalse_ForInvalidAreaCodes(string phone)
    {
        Assert.False(ValidationHelper.IsValidBrazilianCellPhone(phone));
    }

    [Fact]
    public void NormalizeBrazilianCellPhone_ReturnsElevenDigits_WhenInputIsValid()
    {
        Assert.Equal("11987654321", ValidationHelper.NormalizeBrazilianCellPhone("+55 11 98765-4321"));
    }
}

using TecFlow.Util.Text;

namespace TecFlow.Tests.Unit.Util;

public class SlugHelperTests
{
    [Theory]
    [InlineData("@Achadinhos de Aaz", "AchadinhosDeAaz")]
    [InlineData("Loja Homolog", "LojaHomolog")]
    [InlineData("café & chá", "CafeCha")]
    [InlineData("   ", "loja")]
    [InlineData(null, "loja")]
    [InlineData("api", "lojaApi")]
    public void GenerateSlug_ShouldSanitizeFriendlyName(string? name, string expected)
    {
        Assert.Equal(expected, SlugHelper.GenerateSlug(name));
    }

    [Fact]
    public void ShortLinkPublicUrl_ShouldBuildStoreSlugPath()
    {
        var url = ShortLinkPublicUrl.Build("http://localhost:5001/r", "@Achadinhos de Aaz", "abc1234");

        Assert.Equal("http://localhost:5001/AchadinhosDeAaz/abc1234", url);
    }
}

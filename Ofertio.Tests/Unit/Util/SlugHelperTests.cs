using Ofertio.Util.Text;

namespace Ofertio.Tests.Unit.Util;

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

    [Theory]
    [InlineData("http://localhost:5001/AchadinhoCerto/abc1234", "https://dev.ofertio.com.br/", "https://dev.ofertio.com.br/AchadinhoCerto/abc1234")]
    [InlineData("http://127.0.0.1:5001/r/abc1234?src=Telegram", "https://localhost:7002", "https://localhost:7002/r/abc1234?src=Telegram")]
    [InlineData("http://localhost:5001/Loja/abc1234", "https://app.ofertio.com.br/portal/", "https://app.ofertio.com.br/portal/Loja/abc1234")]
    [InlineData("https://tflow.link/Loja/abc1234", "https://dev.ofertio.com.br/", "https://tflow.link/Loja/abc1234")]
    [InlineData("http://localhost:5001/Loja/abc1234", "https://0.0.0.1/", "http://localhost:5001/Loja/abc1234")]
    [InlineData("http://localhost:5001/Loja/abc1234", "app://0.0.0.0/", "http://localhost:5001/Loja/abc1234")]
    [InlineData("http://localhost:5001/Loja/abc1234", null, "http://localhost:5001/Loja/abc1234")]
    [InlineData(" ", "https://dev.ofertio.com.br/", "")]
    public void ShortLinkPublicUrl_RebaseToCurrentHost_ShouldSwapOnlyTheInternalHost(string? shortUrl, string? baseUrl, string expected)
    {
        Assert.Equal(expected, ShortLinkPublicUrl.RebaseToCurrentHost(shortUrl, baseUrl));
    }
}

using TecFlow.SharedUi.Helpers;

namespace TecFlow.Tests.Unit.SharedUi;

public class AffiliateShareLinkBuilderTests
{
    private const string ConvertedUrl = "https://tflow.link/r/abc1234?utm=cadeira gamer & kids";
    private const string Platform = "Shopee";

    [Fact]
    public void BuildWhatsAppUri_ShouldEncodeMessageAndConvertedLink()
    {
        var uri = AffiliateShareLinkBuilder.BuildWhatsAppUri(ConvertedUrl, Platform);

        Assert.StartsWith("https://api.whatsapp.com/send?text=", uri, StringComparison.Ordinal);
        Assert.Contains("text=", uri, StringComparison.Ordinal);
        Assert.DoesNotContain("cadeira gamer & kids", uri, StringComparison.Ordinal);
        Assert.Contains(Uri.EscapeDataString(ConvertedUrl), uri, StringComparison.Ordinal);
        Assert.Contains(Uri.EscapeDataString("Confira este produto na Shopee com meu link de comissão:"), uri, StringComparison.Ordinal);
        Assert.Contains("%26", uri, StringComparison.Ordinal);
        Assert.Contains("%20", uri, StringComparison.Ordinal);

        var query = new Uri(uri).Query.TrimStart('?');
        var textValue = query["text=".Length..];
        var decoded = Uri.UnescapeDataString(textValue);
        Assert.Contains(ConvertedUrl, decoded, StringComparison.Ordinal);
        Assert.Contains("Shopee", decoded, StringComparison.Ordinal);
        Assert.DoesNotContain("&utm=", query, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildTelegramUri_ShouldKeepUrlAndTextAsSeparateEncodedParams()
    {
        var uri = AffiliateShareLinkBuilder.BuildTelegramUri(ConvertedUrl, Platform);

        Assert.StartsWith("https://t.me/share/url?", uri, StringComparison.Ordinal);
        Assert.Contains($"url={Uri.EscapeDataString(ConvertedUrl)}", uri, StringComparison.Ordinal);
        Assert.Contains($"text={Uri.EscapeDataString("Confira este produto na Shopee com meu link de comissão:")}", uri, StringComparison.Ordinal);
        Assert.DoesNotContain("cadeira gamer & kids", uri, StringComparison.Ordinal);

        var parsed = new Uri(uri);
        var pairs = parsed.Query.TrimStart('?').Split('&');
        Assert.Equal(2, pairs.Length);
        Assert.StartsWith("url=", pairs[0], StringComparison.Ordinal);
        Assert.StartsWith("text=", pairs[1], StringComparison.Ordinal);
        Assert.Equal(ConvertedUrl, Uri.UnescapeDataString(pairs[0]["url=".Length..]));
    }

    [Fact]
    public void BuildShareMessage_ShouldJoinIntroAndLinkWithoutBreakingSpecialCharacters()
    {
        var message = AffiliateShareLinkBuilder.BuildShareMessage(ConvertedUrl, "TikTok Shop");

        Assert.StartsWith("Confira este produto na TikTok Shop", message, StringComparison.Ordinal);
        Assert.EndsWith(ConvertedUrl, message, StringComparison.Ordinal);
        Assert.Contains("& kids", message, StringComparison.Ordinal);
    }
}

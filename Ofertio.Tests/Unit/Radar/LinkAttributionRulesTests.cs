using Ofertio.Business.Service.Attribution;
using Xunit;

namespace Ofertio.Tests.Unit.Radar;

public sealed class LinkChannelRulesTests
{
    [Theory]
    [InlineData("w", "WhatsApp")]
    [InlineData("T", "Telegram")]
    [InlineData("e", "Email")]
    [InlineData("p", "PaginaPublica")]
    [InlineData("i", "Instagram")]
    [InlineData("o", "Outro")]
    public void TryFromCode_ShouldMapLetters(string code, string name)
    {
        Assert.True(LinkChannelRules.TryFromCode(code, out var channel));
        Assert.Equal(name, channel.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("x")]
    [InlineData("ww")]
    public void TryFromCode_ShouldRejectUnknown(string? code) =>
        Assert.False(LinkChannelRules.TryFromCode(code, out _));

    [Theory]
    [InlineData("WhatsApp", "WhatsApp")]
    [InlineData("whatsapp", "WhatsApp")]
    [InlineData("E-mail", "Email")]
    [InlineData("Página pública", "PaginaPublica")]
    [InlineData("pagina publica", "PaginaPublica")]
    [InlineData("TikTok", "TikTok")]
    public void FromName_ShouldAcceptLegacyAndLabelVariants(string raw, string expected) =>
        Assert.Equal(expected, LinkChannelRules.FromName(raw)?.Name);

    [Fact]
    public void RouteRegex_ShouldContainEveryLetterOfTheTable() =>
        Assert.Equal("^[wtepio]$", LinkChannelRules.RouteRegex);

    [Fact]
    public void LabelFor_ShouldFallBackToUnknown()
    {
        Assert.Equal("Desconhecido", LinkChannelRules.LabelFor(null));
        Assert.Equal("Página pública", LinkChannelRules.LabelFor("PaginaPublica"));
        Assert.Equal("Direto", LinkChannelRules.LabelFor("Direto"));
    }
}

public sealed class GroupKeyRulesTests
{
    [Fact]
    public void Compute_ShouldBeStableAndLookLikeAKey()
    {
        var first = GroupKeyRules.Compute("segredo", 7, "WhatsApp", "120363@g.us");
        var second = GroupKeyRules.Compute("segredo", 7, "whatsapp", " 120363@g.us ");

        Assert.Equal(first, second);
        Assert.True(GroupKeyRules.IsKey(first));
        Assert.Equal(GroupKeyRules.KeyLength, first.Length);
    }

    [Fact]
    public void Compute_ShouldChangeWithUserChannelGroupAndSecret()
    {
        var baseKey = GroupKeyRules.Compute("segredo", 7, "WhatsApp", "g1");

        Assert.NotEqual(baseKey, GroupKeyRules.Compute("segredo", 8, "WhatsApp", "g1"));
        Assert.NotEqual(baseKey, GroupKeyRules.Compute("segredo", 7, "Telegram", "g1"));
        Assert.NotEqual(baseKey, GroupKeyRules.Compute("segredo", 7, "WhatsApp", "g2"));
        Assert.NotEqual(baseKey, GroupKeyRules.Compute("outro", 7, "WhatsApp", "g1"));
    }

    [Fact]
    public void Compute_ShouldNotLeakTheGroupId()
    {
        var key = GroupKeyRules.Compute("segredo", 7, "WhatsApp", "120363025246125486@g.us");

        Assert.DoesNotContain("120363", key);
        Assert.DoesNotContain("@", key);
    }

    [Fact]
    public void Compute_ShouldNotCollideAcrossAThousandGroups()
    {
        var keys = Enumerable.Range(0, 1000)
            .Select(index => GroupKeyRules.Compute("segredo", 7, "WhatsApp", $"{index}@g.us"))
            .ToHashSet();

        Assert.Equal(1000, keys.Count);
    }

    [Theory]
    [InlineData("120363@g.us")]
    [InlineData("-1001234567")]
    [InlineData("12345678")]
    [InlineData("ABCDEFGH")]
    [InlineData("abc")]
    [InlineData(null)]
    public void IsKey_ShouldRejectJidsChatIdsAndOtherShapes(string? value) =>
        Assert.False(GroupKeyRules.IsKey(value));
}

public sealed class ClickClassifierRulesTests
{
    private const string Chrome = "Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Mobile Safari/537.36";

    [Theory]
    [InlineData("WhatsApp/2.23.20.0")]
    [InlineData("TelegramBot (like TwitterBot)")]
    [InlineData("facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)")]
    [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)")]
    [InlineData("curl/8.4.0")]
    [InlineData("Slackbot-LinkExpanding 1.0")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("desconhecido")]
    public void IsBot_ShouldDetectPreviewFetchersAndMissingUserAgent(string? userAgent) =>
        Assert.True(ClickClassifierRules.IsBot(userAgent));

    [Fact]
    public void IsBot_ShouldKeepRealBrowsers() =>
        Assert.False(ClickClassifierRules.IsBot(Chrome));

    [Fact]
    public void Classify_ExplicitChannelWinsOverEverything()
    {
        var result = ClickClassifierRules.Classify(Chrome + " Instagram 300.0", "https://t.me/x", "WhatsApp");

        Assert.Equal("WhatsApp", result.Channel);
        Assert.Equal(ClickChannelOrigin.Explicit, result.Origin);
        Assert.False(result.IsBot);
    }

    [Fact]
    public void Classify_BotOnExplicitLinkKeepsChannelButIsFlagged()
    {
        var result = ClickClassifierRules.Classify("WhatsApp/2.23.20.0", null, "WhatsApp");

        Assert.Equal("WhatsApp", result.Channel);
        Assert.True(result.IsBot);
    }

    [Fact]
    public void Classify_BotWithoutExplicitChannelIsUnknown()
    {
        var result = ClickClassifierRules.Classify("Googlebot/2.1", null, null);

        Assert.Equal("Desconhecido", result.Channel);
        Assert.True(result.IsBot);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (iPhone) AppleWebKit Instagram 300.0.0", "Instagram")]
    [InlineData("Mozilla/5.0 (Linux; Android 14) [FB_IAB/FB4A;FBAV/450.0]", "Facebook")]
    [InlineData("Mozilla/5.0 (Linux; Android 14) musical_ly_2023", "TikTok")]
    public void Classify_ShouldUseInAppBrowserMarkers(string userAgent, string channel)
    {
        var result = ClickClassifierRules.Classify(userAgent, null, null);

        Assert.Equal(channel, result.Channel);
        Assert.Equal(ClickChannelOrigin.InApp, result.Origin);
    }

    [Theory]
    [InlineData("https://t.me/grupo", "Telegram")]
    [InlineData("https://mail.google.com/mail/u/0/", "Email")]
    [InlineData("https://outlook.live.com/mail/", "Email")]
    [InlineData("https://www.google.com/", "Google")]
    [InlineData("https://t.co/abc", "X")]
    [InlineData("Instagram", "Instagram")]
    public void Classify_ShouldUseReferer(string referrer, string channel)
    {
        var result = ClickClassifierRules.Classify(Chrome, referrer, null);

        Assert.Equal(channel, result.Channel);
        Assert.Equal(ClickChannelOrigin.Referer, result.Origin);
    }

    [Fact]
    public void Classify_WithoutAnyHintIsDirect()
    {
        var result = ClickClassifierRules.Classify(Chrome, null, null);

        Assert.Equal("Direto", result.Channel);
        Assert.False(result.IsBot);
    }
}

public sealed class LinkChannelUrlTests
{
    [Fact]
    public void Apply_ShouldInsertLetterBeforeStoreAndCode() =>
        Assert.Equal(
            "https://localhost:7002/w/97303724/e573hh7",
            LinkChannelUrl.Apply("https://localhost:7002/97303724/e573hh7", 'w'));

    [Fact]
    public void Apply_ShouldReplaceAnExistingLetter() =>
        Assert.Equal(
            "https://localhost:7002/t/97303724/e573hh7",
            LinkChannelUrl.Apply("https://localhost:7002/w/97303724/e573hh7", 't'));

    [Fact]
    public void Apply_ShouldAddGroupKeyOnlyWhenItLooksLikeAKey()
    {
        var key = GroupKeyRules.Compute("s", 1, "WhatsApp", "g");

        Assert.EndsWith("?g=" + key, LinkChannelUrl.Apply("https://x.test/Loja/abc1234", 'w', key));
        Assert.DoesNotContain("?g=", LinkChannelUrl.Apply("https://x.test/Loja/abc1234", 'w', "120363@g.us"));
    }

    [Fact]
    public void Apply_ShouldKeepOtherQueryParameters()
    {
        var result = LinkChannelUrl.Apply("https://x.test/Loja/abc1234?utm=1", 'e');

        Assert.Equal("https://x.test/e/Loja/abc1234?utm=1", result);
    }

    [Theory]
    [InlineData("não é url")]
    [InlineData("https://x.test/so-um")]
    [InlineData("")]
    public void Apply_ShouldReturnInputWhenNotAShortLink(string input) =>
        Assert.Equal(input, LinkChannelUrl.Apply(input, 'w'));

    [Fact]
    public void Apply_ShouldRejectLettersOutsideTheTable() =>
        Assert.Equal("https://x.test/Loja/abc1234", LinkChannelUrl.Apply("https://x.test/Loja/abc1234", 'z'));

    [Fact]
    public void ApplyToMessage_ShouldMarkEveryOwnShortLinkAndNothingElse()
    {
        var message = "Oferta 1: https://localhost:7002/Loja/abc1234.\nOferta 2: https://localhost:7002/Loja/zzz9999!\nSite: https://www.mercadolivre.com.br/p/MLB1";

        var result = LinkChannelUrl.ApplyToMessage(message, "https://localhost:7002", 't', null);

        Assert.Contains("https://localhost:7002/t/Loja/abc1234.", result);
        Assert.Contains("https://localhost:7002/t/Loja/zzz9999!", result);
        Assert.Contains("https://www.mercadolivre.com.br/p/MLB1", result);
        Assert.DoesNotContain("mercadolivre.com.br/t/", result);
    }

    [Fact]
    public void ApplyToMessage_WithoutBaseUrlLeavesTextUntouched() =>
        Assert.Equal("texto https://a.test/x/y", LinkChannelUrl.ApplyToMessage("texto https://a.test/x/y", null, 'w', null));
}

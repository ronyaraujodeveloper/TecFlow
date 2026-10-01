using System.Text.Json;
using TecFlow.Business.Service.WhatsApp;

namespace TecFlow.Tests.Unit.WhatsApp;

public class WhatsAppBotRulesTests
{
    [Fact]
    public void ExtractUrls_ShouldFindHttpLinks()
    {
        var urls = WhatsAppBotRules.ExtractUrls("Olha isso https://shopee.com.br/produto. e http://amzn.to/abc");

        Assert.Equal(2, urls.Count);
        Assert.Contains("https://shopee.com.br/produto", urls);
        Assert.Contains("http://amzn.to/abc", urls);
    }

    [Fact]
    public void TryParseIncoming_ShouldIgnoreFromMe()
    {
        using var document = JsonDocument.Parse("""
            {
              "event": "MESSAGES_UPSERT",
              "instance": "tecflow-u7",
              "data": {
                "key": { "remoteJid": "5511999999999@s.whatsapp.net", "fromMe": true },
                "message": { "conversation": "https://shopee.com.br/x" }
              }
            }
            """);

        var incoming = WhatsAppBotRules.TryParseIncoming(document.RootElement);

        Assert.NotNull(incoming);
        Assert.True(WhatsAppBotRules.ShouldIgnore(incoming!));
    }

    [Fact]
    public void ShouldHandleChat_ShouldSkipGroupsWhenDisabled()
    {
        Assert.False(WhatsAppBotRules.ShouldHandleChat(
            enableBot: true,
            replyPrivate: true,
            replyGroups: false,
            isGroup: true));
        Assert.True(WhatsAppBotRules.ShouldHandleChat(
            enableBot: true,
            replyPrivate: true,
            replyGroups: false,
            isGroup: false));
    }

    [Fact]
    public void IsMessagesUpsert_ShouldAcceptEvolutionEventName()
    {
        using var document = JsonDocument.Parse("""{ "event": "messages.upsert", "data": {} }""");
        Assert.True(WhatsAppBotRules.IsMessagesUpsert(document.RootElement));
    }
}

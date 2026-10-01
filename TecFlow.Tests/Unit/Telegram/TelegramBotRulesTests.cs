using System.Text.Json;
using TecFlow.Business.Service.Telegram;

namespace TecFlow.Tests.Unit.Telegram;

public class TelegramBotRulesTests
{
    [Fact]
    public void TryParseIncoming_ShouldReadPrivateTextAndUrls()
    {
        using var document = JsonDocument.Parse("""
            {
              "update_id": 10,
              "message": {
                "chat": { "id": 55119999, "type": "private" },
                "from": { "is_bot": false },
                "text": "veja https://mercadolivre.com.br/p/MLB1"
              }
            }
            """);

        var incoming = TelegramBotRules.TryParseIncoming(document.RootElement);

        Assert.NotNull(incoming);
        Assert.False(TelegramBotRules.ShouldIgnore(incoming!));
        Assert.Equal("55119999", incoming!.ChatId);
        Assert.Contains("https://mercadolivre.com.br/p/MLB1", TelegramBotRules.ExtractUrls(incoming.Text));
    }

    [Fact]
    public void ShouldIgnore_ShouldSkipGroupsAndBots()
    {
        using var group = JsonDocument.Parse("""
            {
              "message": {
                "chat": { "id": -1001, "type": "supergroup" },
                "from": { "is_bot": false },
                "text": "https://shopee.com.br/x"
              }
            }
            """);
        var incoming = TelegramBotRules.TryParseIncoming(group.RootElement);
        Assert.True(TelegramBotRules.ShouldIgnore(incoming!));
    }

    [Fact]
    public void FormatConvertedReply_ShouldListCommissionLinks()
    {
        var text = TelegramBotRules.FormatConvertedReply(["https://tecflow.local/a"]);
        Assert.Contains("https://tecflow.local/a", text);
    }
}

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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("*****")]
    [InlineData("****************")]
    public void IsPlaceholderToken_ShouldRejectEmptyAndAsterisks(string? token) =>
        Assert.True(TelegramBotRules.IsPlaceholderToken(token));

    [Fact]
    public void IsPlaceholderToken_ShouldAcceptBotFatherToken() =>
        Assert.False(TelegramBotRules.IsPlaceholderToken("8743142006:AAHdemo"));

    [Fact]
    public void ResolveUiStatus_ShouldUseDispatchModeWhenWebhookFailsAndChatIdExists()
    {
        Assert.Equal(
            TelegramBotRules.DispatchModeLabel,
            TelegramBotRules.ResolveUiStatus(true, true, true, true, webhookRegistered: false));
        Assert.Equal(
            TelegramBotRules.ConnectedLabel,
            TelegramBotRules.ResolveUiStatus(true, true, true, true, webhookRegistered: true));
    }

    [Fact]
    public void IsWebhookRegistered_ShouldReadFlagAndLegacyUrl()
    {
        Assert.False(TelegramBotRules.IsWebhookRegistered("""{"webhookRegistered":false}"""));
        Assert.True(TelegramBotRules.IsWebhookRegistered("""{"webhookRegistered":true}"""));
        Assert.True(TelegramBotRules.IsWebhookRegistered("""{"webhookUrl":"https://api.tecflow.local/hook"}"""));
    }

    [Fact]
    public void FormatConvertedReply_ShouldListCommissionLinks()
    {
        var text = TelegramBotRules.FormatConvertedReply(["https://tecflow.local/a"]);
        Assert.Contains("https://tecflow.local/a", text);
    }
}

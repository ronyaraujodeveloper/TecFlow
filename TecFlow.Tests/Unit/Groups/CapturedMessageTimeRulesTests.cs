using System.Text.Json;
using TecFlow.Business.Service.Groups;
using TecFlow.Business.Service.Telegram;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Entities;

namespace TecFlow.Tests.Unit.Groups;

public class CapturedMessageTimeRulesTests
{
    [Fact]
    public void OrderByCapturedAtDescending_ShouldUseMessageTimeInsteadOfInsertTime()
    {
        var rows = new[]
        {
            Row(1, "2026-10-18 12:44:48", "2026-10-18 12:45:12"),
            Row(2, "2026-10-18 03:23:21", "2026-10-18 12:31:56"),
            Row(3, "2026-10-18 12:20:35", "2026-10-18 12:21:27"),
            Row(4, "2026-10-18 12:14:46", "2026-10-18 12:15:31"),
            Row(5, "2026-10-18 11:44:44", "2026-10-18 12:02:18"),
            Row(6, "2026-10-18 11:14:48", "2026-10-18 12:02:27")
        };

        var labels = rows.AsQueryable()
            .OrderByCapturedAtDescending()
            .Select(item => CapturedMessageTimeRules.ToBrasilia(item.ReceivedAt).ToString("HH:mm"))
            .ToList();

        Assert.Equal(["09:44", "09:20", "09:14", "08:44", "08:14", "00:23"], labels);
    }

    [Fact]
    public void OrderByCapturedAtDescending_ShouldBreakTiesByNewestId()
    {
        var rows = new[]
        {
            Row(10, "2026-10-18 11:14:48", "2026-10-18 12:02:21"),
            Row(11, "2026-10-18 11:14:48", "2026-10-18 12:02:27")
        };

        var ids = rows.AsQueryable().OrderByCapturedAtDescending().Select(item => item.Id).ToList();

        Assert.Equal([11, 10], ids);
    }

    [Fact]
    public void NormalizeUtc_ShouldTreatDatabaseValuesAsUtc()
    {
        var fromDatabase = new DateTime(2026, 10, 18, 12, 44, 0, DateTimeKind.Unspecified);

        var utc = CapturedMessageTimeRules.NormalizeUtc(fromDatabase);

        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(fromDatabase.Ticks, utc.Ticks);
    }

    [Fact]
    public void ToBrasilia_ShouldShiftUtcByThreeHours()
    {
        var utc = new DateTime(2026, 10, 18, 3, 23, 21, DateTimeKind.Utc);

        Assert.Equal("18/10/2026 00:23", CapturedMessageTimeRules.ToBrasilia(utc).ToString("dd/MM/yyyy HH:mm"));
    }

    [Theory]
    [InlineData(1792326288L, "2026-10-18 12:24:48")]
    [InlineData(1792326288000L, "2026-10-18 12:24:48")]
    public void FromUnixSeconds_ShouldAcceptSecondsAndMilliseconds(long value, string expected)
    {
        var parsed = CapturedMessageTimeRules.FromUnixSeconds(value);

        Assert.Equal(expected, parsed?.ToString("yyyy-MM-dd HH:mm:ss"));
        Assert.Equal(DateTimeKind.Utc, parsed?.Kind);
    }

    [Fact]
    public void ResolveReceivedAtUtc_ShouldPreferMessageTime_AndRejectImplausibleValues()
    {
        var now = new DateTime(2026, 10, 18, 13, 0, 0, DateTimeKind.Utc);
        var sent = new DateTime(2026, 10, 18, 3, 23, 0, DateTimeKind.Utc);

        Assert.Equal(sent, CapturedMessageTimeRules.ResolveReceivedAtUtc(sent, now));
        Assert.Equal(now, CapturedMessageTimeRules.ResolveReceivedAtUtc(null, now));
        Assert.Equal(now, CapturedMessageTimeRules.ResolveReceivedAtUtc(now.AddHours(2), now));
        Assert.Equal(now, CapturedMessageTimeRules.ResolveReceivedAtUtc(now.AddYears(-3), now));
    }

    [Theory]
    [InlineData("1792326288")]
    [InlineData("\"1792326288\"")]
    [InlineData("{ \"low\": 1792326288, \"high\": 0, \"unsigned\": false }")]
    public void WhatsAppTryParseIncoming_ShouldReadMessageTimestamp(string timestamp)
    {
        using var document = JsonDocument.Parse($$"""
            {
              "event": "MESSAGES_UPSERT",
              "instance": "tecflow-u7",
              "data": {
                "key": { "remoteJid": "120363000000000000@g.us", "fromMe": false, "id": "ABC" },
                "message": { "conversation": "Oferta https://shopee.com.br/x" },
                "messageTimestamp": {{timestamp}}
              }
            }
            """);

        var incoming = WhatsAppBotRules.TryParseIncoming(document.RootElement);

        Assert.Equal(new DateTime(2026, 10, 18, 12, 24, 48, DateTimeKind.Utc), incoming?.SentAtUtc);
    }

    [Fact]
    public void TelegramTryParseIncoming_ShouldReadMessageDate()
    {
        using var document = JsonDocument.Parse("""
            {
              "update_id": 1,
              "message": {
                "message_id": 99,
                "date": 1792326288,
                "chat": { "id": -100123, "type": "supergroup", "title": "Achados" },
                "from": { "is_bot": false },
                "text": "Oferta https://shopee.com.br/x"
              }
            }
            """);

        var incoming = TelegramBotRules.TryParseIncoming(document.RootElement);

        Assert.Equal(new DateTime(2026, 10, 18, 12, 24, 48, DateTimeKind.Utc), incoming?.SentAtUtc);
    }

    private static GroupCapturedMessage Row(int id, string receivedAtUtc, string createdAtUtc) =>
        new()
        {
            Id = id,
            ReceivedAt = DateTime.SpecifyKind(DateTime.Parse(receivedAtUtc), DateTimeKind.Utc),
            CreatedAt = DateTime.SpecifyKind(DateTime.Parse(createdAtUtc), DateTimeKind.Utc)
        };
}

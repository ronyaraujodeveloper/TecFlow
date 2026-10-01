using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.API.Controllers;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Tests.Unit.Telegram;

public class TelegramWebhookControllerTests
{
    [Fact]
    public async Task ReceiveAsync_ShouldForwardPayloadToProcessor()
    {
        var processor = new Mock<ITelegramMessageProcessor>();
        var controller = new TelegramWebhookController(
            processor.Object,
            NullLogger<TelegramWebhookController>.Instance);

        using var document = JsonDocument.Parse("""
            {
              "update_id": 1,
              "message": {
                "chat": { "id": 99, "type": "private" },
                "text": "https://shopee.com.br/x"
              }
            }
            """);

        var action = await controller.ReceiveAsync(7, document.RootElement, CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        processor.Verify(
            service => service.ProcessAsync(7, It.IsAny<JsonElement>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

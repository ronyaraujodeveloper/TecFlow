using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TecFlow.API.Controllers;
using TecFlow.Business.Integrations.WhatsApp;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Tests.Unit.WhatsApp;

public class WhatsAppWebhookControllerTests
{
    [Fact]
    public async Task ReceiveAsync_ShouldSkipProcessorWhenFromMe()
    {
        var processor = new Mock<IWhatsAppMessageProcessor>();
        var controller = new WhatsAppWebhookController(
            processor.Object,
            Options.Create(new EvolutionApiOptions()),
            NullLogger<WhatsAppWebhookController>.Instance);

        using var document = JsonDocument.Parse("""
            {
              "event": "MESSAGES_UPSERT",
              "instance": "tecflow-u1",
              "data": {
                "key": { "remoteJid": "5511988887777@s.whatsapp.net", "fromMe": true },
                "message": { "conversation": "https://mercadolivre.com.br/p/MLB1" }
              }
            }
            """);

        var action = await controller.ReceiveAsync(document.RootElement, CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        processor.Verify(
            service => service.ProcessAsync(It.IsAny<JsonElement>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReceiveAsync_ShouldIgnoreUnknownEvents()
    {
        var processor = new Mock<IWhatsAppMessageProcessor>();
        var controller = new WhatsAppWebhookController(
            processor.Object,
            Options.Create(new EvolutionApiOptions()),
            NullLogger<WhatsAppWebhookController>.Instance);

        using var document = JsonDocument.Parse("""{ "event": "CONNECTION_UPDATE" }""");
        var action = await controller.ReceiveAsync(document.RootElement, CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        processor.Verify(
            service => service.ProcessAsync(It.IsAny<JsonElement>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

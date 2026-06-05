using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.API.Controllers;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Orders;
using TecFlow.Business.Integrations.Webhooks;
using TecFlow.Core.Enums;

namespace TecFlow.Tests.Unit.Controllers;

public class MarketplaceWebhookControllerTests
{
    [Fact]
    public async Task ShopeeReceiveAsync_ShouldReturnUnauthorized_WhenSignatureIsInvalid()
    {
        // Arrange
        const string body = """{"code":3,"shop_id":1}""";
        var verifier = new Mock<IMarketplaceWebhookSignatureVerifier>();
        verifier.Setup(v => v.VerifyShopeePush(body, It.IsAny<string>())).Returns(false);

        var controller = new ShopeeWebhookController(
            verifier.Object,
            new Mock<IMarketplaceOrderService>().Object,
            NullLogger<ShopeeWebhookController>.Instance);

        SetupJsonBody(controller, body, authorization: "invalid");

        // Act
        var result = await controller.ReceiveAsync(CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task TikTokReceiveAsync_ShouldReturnOk_WhenSignatureValidAndOrderProcessed()
    {
        // Arrange
        const string body = """{"type":"order_status_change","shop_id":"9","data":{"order_id":"1","order_status":"AWAITING_SHIPMENT"}}""";
        var verifier = new Mock<IMarketplaceWebhookSignatureVerifier>();
        verifier.Setup(v => v.VerifyTikTokShopPush(body, "valid-sig")).Returns(true);

        var orders = new Mock<IMarketplaceOrderService>();
        orders.Setup(o => o.ProcessWebhookOrderAsync(body, MarketplaceType.TikTokShop, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketplaceOrderResult { Status = true, Descricao = "OK" });

        var controller = new TikTokShopWebhookController(
            verifier.Object,
            orders.Object,
            NullLogger<TikTokShopWebhookController>.Instance);

        SetupJsonBody(controller, body, webhookSignature: "valid-sig");

        // Act
        var result = await controller.ReceiveAsync(CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    private static void SetupJsonBody(
        ControllerBase controller,
        string json,
        string? authorization = null,
        string? webhookSignature = null)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream(bytes);
        var context = new DefaultHttpContext { Request = { Body = stream } };
        context.Request.ContentLength = bytes.Length;
        context.Request.EnableBuffering();

        if (authorization is not null)
        {
            context.Request.Headers.Authorization = authorization;
        }

        if (webhookSignature is not null)
        {
            context.Request.Headers["Webhook-Signature"] = webhookSignature;
        }

        controller.ControllerContext = new ControllerContext { HttpContext = context };
    }
}

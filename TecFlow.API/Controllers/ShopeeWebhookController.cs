using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Integrations.Orders;
using TecFlow.Business.Integrations.Webhooks;
using TecFlow.Core.Enums;

namespace TecFlow.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/webhooks/shopee")]
public class ShopeeWebhookController : ControllerBase
{
    private readonly IMarketplaceWebhookSignatureVerifier _signatureVerifier;
    private readonly IMarketplaceOrderService _orderService;
    private readonly ILogger<ShopeeWebhookController> _logger;

    public ShopeeWebhookController(
        IMarketplaceWebhookSignatureVerifier signatureVerifier,
        IMarketplaceOrderService orderService,
        ILogger<ShopeeWebhookController> logger)
    {
        _signatureVerifier = signatureVerifier;
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveAsync(CancellationToken cancellationToken)
    {
        var rawBody = await ReadRawBodyAsync();
        var authorization = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization))
        {
            authorization = Request.Headers["Authorization"].ToString();
        }

        if (!_signatureVerifier.VerifyShopeePush(rawBody, authorization))
        {
            _logger.LogWarning("Webhook Shopee rejeitado: assinatura inválida.");
            return Unauthorized(new { message = "Assinatura inválida." });
        }

        var result = await _orderService.ProcessWebhookOrderAsync(
            rawBody,
            MarketplaceType.Shopee,
            cancellationToken);

        if (!result.Status)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    private async Task<string> ReadRawBodyAsync()
    {
        Request.EnableBuffering();
        Request.Body.Position = 0;
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        Request.Body.Position = 0;
        return body;
    }
}

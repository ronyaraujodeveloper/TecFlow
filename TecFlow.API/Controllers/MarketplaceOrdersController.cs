using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Orders;
using TecFlow.Core.Enums;

namespace TecFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/marketplace-orders")]
public class MarketplaceOrdersController : ControllerBase
{
    private readonly IMarketplaceOrderService _orderService;

    public MarketplaceOrdersController(IMarketplaceOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Polling de contingência para pedidos não recebidos via webhook.</summary>
    [HttpPost("poll")]
    public async Task<ActionResult<MarketplaceOrderResult>> PollAsync(
        [FromQuery] string shopId,
        [FromQuery] MarketplaceType type,
        [FromQuery] int hoursBack = 24,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.SyncMissingOrdersPollingAsync(
            shopId,
            type,
            hoursBack,
            cancellationToken);

        return result.Status ? Ok(result) : BadRequest(result);
    }
}

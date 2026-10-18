using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Catalog;
using Ofertio.Core.Enums;

namespace Ofertio.API.Controllers;

[ApiController]
[Authorize]
[Route("api/marketplace-products")]
public class MarketplaceProductsController : ControllerBase
{
    private readonly IMarketplaceProductService _marketplaceProductService;

    public MarketplaceProductsController(IMarketplaceProductService marketplaceProductService)
    {
        _marketplaceProductService = marketplaceProductService;
    }

    /// <summary>Sincroniza catálogo da Shopee ou TikTok Shop e retorna ProductResponseDto padronizado.</summary>
    [HttpGet("sync")]
    public async Task<ActionResult<ProductResponseDto>> SyncAsync(
        [FromQuery] string shopId,
        [FromQuery] MarketplaceType type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _marketplaceProductService.FetchProductsFromPlatformAsync(
            shopId,
            type,
            page,
            pageSize,
            cancellationToken);

        if (!result.Status)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;

namespace TecFlow.Orquestrador.Controllers;

[ApiController]
[Route("api/propaganda/produtos")]
[Authorize]
public class AdvertisingProductsController : ControllerBase
{
    private readonly IAdvertisingProductService _advertisingProductService;

    public AdvertisingProductsController(IAdvertisingProductService advertisingProductService)
    {
        _advertisingProductService = advertisingProductService;
    }

    [HttpGet]
    public async Task<ActionResult<GlobalAdvertisingProductResponseDto>> ListAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GlobalAdvertisingProductResponseDto { Status = false, Descricao = "Não autorizado." });
        }

        var items = await _advertisingProductService.GetByOwnerAsync(userId, cancellationToken);
        return Ok(new GlobalAdvertisingProductResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = items.ToList()
        });
    }

    [HttpPost]
    public async Task<ActionResult<GlobalAdvertisingProductResponseDto>> CreateAsync(
        [FromBody] GlobalAdvertisingProductDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GlobalAdvertisingProductResponseDto { Status = false, Descricao = "Não autorizado." });
        }

        try
        {
            var created = await _advertisingProductService.CreateGlobalProductAsync(userId, dto, cancellationToken);
            return Ok(new GlobalAdvertisingProductResponseDto
            {
                Status = true,
                Descricao = "Produto de propaganda criado.",
                Data = created
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new GlobalAdvertisingProductResponseDto { Status = false, Descricao = ex.Message });
        }
    }

    [HttpGet("{globalProductId:guid}/payload")]
    public async Task<ActionResult<GlobalAdvertisingProductResponseDto>> GetPayloadAsync(
        Guid globalProductId,
        [FromQuery] MarketplaceType platform,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out _))
        {
            return Unauthorized(new GlobalAdvertisingProductResponseDto { Status = false, Descricao = "Não autorizado." });
        }

        try
        {
            var payload = await _advertisingProductService.GenerateOptimizedPayloadForPostAsync(
                globalProductId,
                platform,
                cancellationToken);

            return Ok(new GlobalAdvertisingProductResponseDto
            {
                Status = true,
                Descricao = "Payload gerado.",
                Payload = payload
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new GlobalAdvertisingProductResponseDto { Status = false, Descricao = ex.Message });
        }
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out userId);
    }
}

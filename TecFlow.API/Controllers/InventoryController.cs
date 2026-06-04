using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Inventory;
using TecFlow.Database;
using TecFlow.Database.Filter;
using TecFlow.Database.Pagin;

namespace TecFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/estoque")]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IInventoryService _inventoryService;

    public InventoryController(AppDbContext context, IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<ActionResult<InventoryResponseDto>> GetByFilterAsync([FromQuery] InventoryFilter filter)
    {
        var items = await _context.Inventories
            .Include(i => i.Product)
            .OrderBy(i => i.ProductId)
            .ToListAsync();

        var filtered = items.ApplyFilter(filter);
        var (page, meta) = PagedListHelper.Slice(filtered, filter);

        return Ok(new InventoryResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = page,
            Paging = PagingInfoDto.FromMeta(meta)
        });
    }

    [HttpGet("produto/{productId:int}")]
    public async Task<ActionResult<InventoryResponseDto>> GetByProductAsync(int productId)
    {
        var inventory = await _inventoryService.GetOrCreateAsync(productId);
        return Ok(new InventoryResponseDto { Status = true, Descricao = "OK", Data = inventory });
    }

    [HttpPut("produto/{productId:int}/minimo")]
    public async Task<ActionResult<InventoryResponseDto>> SetMinimumStockAsync(int productId, [FromBody] InventoryDto dto)
    {
        var inventory = await _inventoryService.GetOrCreateAsync(productId);
        inventory.MinimumStock = Math.Max(0, dto.MinimumStock);
        inventory.Touch();
        await _context.SaveChangesAsync();

        return Ok(new InventoryResponseDto
        {
            Status = true,
            Descricao = "Estoque mínimo atualizado.",
            Data = inventory
        });
    }

    [HttpPost("entrada-compra")]
    public async Task<ActionResult<InventoryResponseDto>> PurchaseEntryAsync([FromBody] InventoryPurchaseEntryDto dto)
    {
        try
        {
            var inventory = await _inventoryService.RegisterPurchaseEntryAsync(
                dto.ProductId,
                dto.Quantity,
                dto.Description);

            return Ok(new InventoryResponseDto
            {
                Status = true,
                Descricao = "Entrada por compra registrada.",
                Data = inventory
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new InventoryResponseDto { Status = false, Descricao = ex.Message });
        }
    }

    [HttpPost("ajuste-manual")]
    public async Task<ActionResult<InventoryResponseDto>> ManualAdjustmentAsync([FromBody] InventoryAdjustmentDto dto)
    {
        try
        {
            var inventory = await _inventoryService.RegisterManualAdjustmentAsync(
                dto.ProductId,
                dto.QuantityDelta,
                dto.Description);

            return Ok(new InventoryResponseDto
            {
                Status = true,
                Descricao = "Ajuste manual registrado.",
                Data = inventory
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new InventoryResponseDto { Status = false, Descricao = ex.Message });
        }
    }
}

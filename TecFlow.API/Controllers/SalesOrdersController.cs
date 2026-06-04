using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Domain.Inventory;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Sales;
using TecFlow.Database.Filter;
using TecFlow.Database.Pagin;

namespace TecFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/vendas/pedidos")]
public class SalesOrdersController : ControllerBase
{
    private readonly ISalesOrderRepository _orderRepository;
    private readonly ISalesOrderService _salesOrderService;
    private readonly IInvoiceOrchestrator _invoiceOrchestrator;

    public SalesOrdersController(
        ISalesOrderRepository orderRepository,
        ISalesOrderService salesOrderService,
        IInvoiceOrchestrator invoiceOrchestrator)
    {
        _orderRepository = orderRepository;
        _salesOrderService = salesOrderService;
        _invoiceOrchestrator = invoiceOrchestrator;
    }

    [HttpGet]
    public async Task<ActionResult<SalesOrderResponseDto>> GetByFilterAsync([FromQuery] SalesOrderFilter filter)
    {
        var filtered = (await _orderRepository.ListAsync()).ApplyFilter(filter);
        var (items, meta) = PagedListHelper.Slice(filtered, filter);

        return Ok(new SalesOrderResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = items,
            Paging = PagingInfoDto.FromMeta(meta)
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesOrderResponseDto>> GetByIdAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id, includeItems: true);
        if (order is null)
        {
            return NotFound(new SalesOrderResponseDto { Status = false, Descricao = "Pedido não encontrado." });
        }

        return Ok(new SalesOrderResponseDto { Status = true, Descricao = "OK", Data = order });
    }

    [HttpPost]
    public async Task<ActionResult<SalesOrderResponseDto>> CreateAsync([FromBody] SalesOrderDto dto)
    {
        try
        {
            var order = await _salesOrderService.CreateOrderAsync(dto);
            return Ok(new SalesOrderResponseDto
            {
                Status = true,
                Descricao = "Pedido criado com sucesso.",
                Data = order
            });
        }
        catch (InsufficientStockException ex)
        {
            return Conflict(new SalesOrderResponseDto { Status = false, Descricao = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new SalesOrderResponseDto { Status = false, Descricao = ex.Message });
        }
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<SalesOrderResponseDto>> UpdateStatusAsync(Guid id, [FromBody] SalesOrderStatusDto dto)
    {
        try
        {
            var order = await _salesOrderService.TransitionStatusAsync(id, dto.Status);
            return Ok(new SalesOrderResponseDto
            {
                Status = true,
                Descricao = "Status atualizado.",
                Data = order
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new SalesOrderResponseDto { Status = false, Descricao = ex.Message });
        }
    }

    [HttpPost("{id:guid}/faturar")]
    public async Task<ActionResult<InvoicePreparationResponseDto>> PrepareInvoiceAsync(Guid id)
    {
        try
        {
            var result = await _invoiceOrchestrator.PrepareInvoiceAsync(id);
            if (!result.Status)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new InvoicePreparationResponseDto { Status = false, Descricao = ex.Message });
        }
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.API.Controllers;

[Authorize]
[ApiController]
[Route("api/integracoes/grupos/monitorados")]
public sealed class MonitoredGroupsController : ControllerBase
{
    private readonly IMonitoredGroupService _service;

    public MonitoredGroupsController(IMonitoredGroupService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> ListAsync(
        [FromQuery] int hours = 24,
        [FromQuery] string? groupKey = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        return Ok(await _service.ListAsync(userId, hours, groupKey, cancellationToken));
    }

    [HttpPost("sincronizar")]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> SyncAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _service.SyncAsync(userId, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{offerId:int}/validar")]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> ValidateAsync(
        int offerId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _service.ValidateAsync(userId, offerId, cancellationToken);
        return result.Status ? Ok(result) : NotFound(result);
    }

    [HttpPost("{offerId:int}/clonar")]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> CloneAsync(
        int offerId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _service.CloneAsync(userId, offerId, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out userId);
    }

    private static MonitoredGroupsResponseDto Fail(string message) =>
        new() { Status = false, Descricao = message };
}

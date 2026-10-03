using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.API.Controllers;

[Authorize]
[ApiController]
[Route("api/integracoes/whatsapp")]
public sealed class WhatsAppBroadcastController : ControllerBase
{
    private readonly IWhatsAppBroadcastService _broadcasts;

    public WhatsAppBroadcastController(IWhatsAppBroadcastService broadcasts)
    {
        _broadcasts = broadcasts;
    }

    [HttpGet("grupos")]
    public async Task<ActionResult<WhatsAppBroadcastResponseDto>> ListGroupsAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        return Ok(await _broadcasts.ListGroupsAsync(userId, cancellationToken));
    }

    [HttpPost("grupos/sincronizar")]
    public async Task<ActionResult<WhatsAppBroadcastResponseDto>> SyncGroupsAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _broadcasts.SyncGroupsAsync(userId, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpGet("campanhas")]
    public async Task<ActionResult<WhatsAppBroadcastResponseDto>> ListCampaignsAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        return Ok(await _broadcasts.ListCampaignsAsync(userId, cancellationToken));
    }

    [HttpPost("campanhas")]
    public async Task<ActionResult<WhatsAppBroadcastResponseDto>> ScheduleAsync(
        [FromBody] WhatsAppScheduleCampaignDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _broadcasts.ScheduleAsync(userId, request ?? new WhatsAppScheduleCampaignDto(), cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPut("campanhas/{id:int}")]
    public async Task<ActionResult<WhatsAppBroadcastResponseDto>> UpdateAsync(
        int id,
        [FromBody] UpdateAgendamentoCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        command ??= new UpdateAgendamentoCommand();
        command.Id = id;
        var result = await _broadcasts.UpdateAsync(userId, command, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("campanhas/{id:int}")]
    public async Task<ActionResult<WhatsAppBroadcastResponseDto>> DeleteCampaignAsync(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _broadcasts.DeleteCampaignAsync(userId, id, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out userId);
    }

    private static WhatsAppBroadcastResponseDto Fail(string message) =>
        new() { Status = false, Descricao = message };
}

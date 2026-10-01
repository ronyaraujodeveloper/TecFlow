using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.API.Controllers;

[Authorize]
[ApiController]
[Route("api/integracoes/telegram")]
public sealed class TelegramBroadcastController : ControllerBase
{
    private readonly ITelegramBroadcastService _broadcasts;

    public TelegramBroadcastController(ITelegramBroadcastService broadcasts)
    {
        _broadcasts = broadcasts;
    }

    [HttpGet("campanhas")]
    public async Task<ActionResult<TelegramBroadcastResponseDto>> ListCampaignsAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        return Ok(await _broadcasts.ListCampaignsAsync(userId, cancellationToken));
    }

    [HttpPost("campanhas")]
    public async Task<ActionResult<TelegramBroadcastResponseDto>> ScheduleAsync(
        [FromBody] TelegramScheduleCampaignDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _broadcasts.ScheduleAsync(
            userId,
            request ?? new TelegramScheduleCampaignDto(),
            cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out userId);
    }

    private static TelegramBroadcastResponseDto Fail(string message) =>
        new() { Status = false, Descricao = message };
}

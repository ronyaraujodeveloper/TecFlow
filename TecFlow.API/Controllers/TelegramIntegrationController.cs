using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.API.Controllers;

[Authorize]
[ApiController]
[Route("api/integracoes/telegram")]
public sealed class TelegramIntegrationController : ControllerBase
{
    private readonly ITelegramIntegrationService _telegram;

    public TelegramIntegrationController(ITelegramIntegrationService telegram)
    {
        _telegram = telegram;
    }

    [HttpGet]
    public async Task<ActionResult<TelegramIntegrationResponseDto>> GetMineAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _telegram.GetMineAsync(userId, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPut]
    public async Task<ActionResult<TelegramIntegrationResponseDto>> SaveAsync(
        [FromBody] SaveTelegramIntegrationDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _telegram.SaveAsync(userId, request ?? new SaveTelegramIntegrationDto(), cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPost("conectar")]
    public async Task<ActionResult<TelegramIntegrationResponseDto>> ConnectAsync(
        [FromBody] SaveTelegramIntegrationDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _telegram.ConnectAsync(userId, request ?? new SaveTelegramIntegrationDto(), cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPost("userbot/solicitar-codigo")]
    public async Task<ActionResult<TelegramIntegrationResponseDto>> RequestUserBotCodeAsync(
        [FromBody] SaveTelegramIntegrationDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _telegram.RequestUserBotCodeAsync(
            userId,
            request ?? new SaveTelegramIntegrationDto(),
            cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPost("userbot/confirmar")]
    public async Task<ActionResult<TelegramIntegrationResponseDto>> ConfirmUserBotAsync(
        [FromBody] SaveTelegramIntegrationDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _telegram.ConfirmUserBotAsync(
            userId,
            request ?? new SaveTelegramIntegrationDto(),
            cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out userId);
    }

    private static TelegramIntegrationResponseDto Fail(string message) =>
        new() { Status = false, Descricao = message };
}

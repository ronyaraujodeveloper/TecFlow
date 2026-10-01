using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.API.Controllers;

[Authorize]
[ApiController]
[Route("api/integracoes/whatsapp")]
public sealed class WhatsAppIntegrationController : ControllerBase
{
    private readonly IWhatsAppSessionService _sessions;

    public WhatsAppIntegrationController(IWhatsAppSessionService sessions)
    {
        _sessions = sessions;
    }

    [HttpGet]
    public async Task<ActionResult<WhatsAppIntegrationResponseDto>> GetMineAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _sessions.GetMineAsync(userId, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPost("conectar")]
    public async Task<ActionResult<WhatsAppIntegrationResponseDto>> ConnectAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _sessions.ConnectAsync(userId, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpGet("status")]
    public async Task<ActionResult<WhatsAppIntegrationResponseDto>> StatusAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _sessions.RefreshStatusAsync(userId, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out userId);
    }

    private static WhatsAppIntegrationResponseDto Fail(string message) =>
        new() { Status = false, Descricao = message };
}

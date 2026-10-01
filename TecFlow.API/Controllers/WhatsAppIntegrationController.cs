using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.WhatsApp;

namespace TecFlow.API.Controllers;

[Authorize]
[ApiController]
[Route("api/integracoes/whatsapp")]
public sealed class WhatsAppIntegrationController : ControllerBase
{
    private readonly IWhatsAppSessionService _sessions;
    private readonly ILogger<WhatsAppIntegrationController> _logger;

    public WhatsAppIntegrationController(
        IWhatsAppSessionService sessions,
        ILogger<WhatsAppIntegrationController> logger)
    {
        _sessions = sessions;
        _logger = logger;
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

        try
        {
            var result = await _sessions.ConnectAsync(userId, cancellationToken);
            return result.Status ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno ao conectar WhatsApp. UserId={UserId}", userId);
            return BadRequest(Fail(WhatsAppSessionRules.EvolutionUnreachableMessage));
        }
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

    [HttpPut("bot")]
    public async Task<ActionResult<WhatsAppIntegrationResponseDto>> UpdateBotAsync(
        [FromBody] WhatsAppBotPreferencesDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _sessions.UpdateBotPreferencesAsync(
            userId,
            request ?? new WhatsAppBotPreferencesDto(),
            cancellationToken);
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

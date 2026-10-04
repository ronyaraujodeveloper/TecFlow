using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Infrastructure.Interfaces;

namespace TecFlow.API.Controllers;

[Authorize]
[ApiController]
[Route("api/integracoes/grupos/monitorados")]
public sealed class MonitoredGroupsController : ControllerBase
{
    private const int LocalFallbackUserId = 1;

    private readonly IMonitoredGroupService _service;
    private readonly IUserContextProvider _userContext;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<MonitoredGroupsController> _logger;

    public MonitoredGroupsController(
        IMonitoredGroupService service,
        IUserContextProvider userContext,
        IHostEnvironment environment,
        ILogger<MonitoredGroupsController> logger)
    {
        _service = service;
        _userContext = userContext;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> ListAsync(
        [FromQuery] int hours = 24,
        [FromQuery] string? groupKey = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = ResolveUserId();
            return Ok(await _service.ListAsync(userId, hours, groupKey, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar grupos monitorados");
            return Ok(Fail(Describe(ex)));
        }
    }

    [HttpPost("sincronizar")]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> SyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            var userId = ResolveUserId();
            var result = await _service.SyncAsync(userId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar grupos monitorados");
            return Ok(Fail(Describe(ex)));
        }
    }

    [HttpPost("{offerId:int}/validar")]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> ValidateAsync(
        int offerId,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var result = await _service.ValidateAsync(userId, offerId, cancellationToken);
        return result.Status ? Ok(result) : NotFound(result);
    }

    [HttpPost("{offerId:int}/clonar")]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> CloneAsync(
        int offerId,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var result = await _service.CloneAsync(userId, offerId, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    private int ResolveUserId()
    {
        var fromContext = _userContext.GetCurrentUserId();
        if (fromContext is > 0)
        {
            return fromContext.Value;
        }

        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out var userId) && userId > 0)
        {
            return userId;
        }

        _logger.LogWarning(
            "Usuário não autenticado ou sem UserId. Usando fallback local UserId={UserId}.",
            LocalFallbackUserId);
        return LocalFallbackUserId;
    }

    private string Describe(Exception ex)
    {
        if (_environment.IsDevelopment() || _environment.IsEnvironment("Homologacao"))
        {
            return string.IsNullOrWhiteSpace(ex.Message)
                ? "Erro ao sincronizar grupos monitorados."
                : ex.Message;
        }

        return "Não foi possível sincronizar os grupos monitorados. Tente novamente.";
    }

    private static MonitoredGroupsResponseDto Fail(string message) =>
        new() { Status = false, Descricao = message };
}

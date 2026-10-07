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
    private readonly IUserBotSyncStatusService _syncStatus;
    private readonly IUserContextProvider _userContext;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<MonitoredGroupsController> _logger;

    public MonitoredGroupsController(
        IMonitoredGroupService service,
        IUserBotSyncStatusService syncStatus,
        IUserContextProvider userContext,
        IHostEnvironment environment,
        ILogger<MonitoredGroupsController> logger)
    {
        _service = service;
        _syncStatus = syncStatus;
        _userContext = userContext;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> ListAsync(
        [FromQuery] int hours = 24,
        [FromQuery] string? groupKey = null,
        [FromQuery] string? channel = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 25,
        [FromQuery] bool ignored = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = ResolveUserId();
            return Ok(await _service.ListAsync(userId, hours, groupKey, channel, skip, take, ignored, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar grupos monitorados");
            return Ok(Fail(Describe(ex)));
        }
    }

    [HttpGet("status")]
    public ActionResult<UserBotSyncStatusDto> GetSyncStatus()
    {
        return Ok(_syncStatus.Get(ResolveUserId()));
    }

    [HttpPost("sincronizar")]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> SyncAsync(
        [FromQuery] string? channel = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = ResolveUserId();
            var result = await _service.SyncAsync(userId, channel, cancellationToken);
            return result.Status ? Accepted(result) : Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar grupos monitorados");
            return Ok(Fail(Describe(ex)));
        }
    }

    [HttpPost("midia/priorizar")]
    public async Task<ActionResult<object>> PrioritizeMediaAsync(
        [FromBody] PrioritizeMonitoredMediaRequest? request,
        CancellationToken cancellationToken = default)
    {
        var queued = await _service.PrioritizeVisibleMediaAsync(
            ResolveUserId(),
            request?.OfferIds ?? [],
            cancellationToken);
        return Ok(new MonitoredGroupsResponseDto
        {
            Status = true,
            Descricao = queued > 0
                ? $"{queued} foto(s) da página visível foram priorizadas."
                : "Nenhuma foto pendente na página visível."
        });
    }

    [HttpPost("midia/vincular-disco")]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> LinkDiskMediaAsync(
        CancellationToken cancellationToken = default)
    {
        var linked = await _service.LinkExistingDownloadedImagesAsync(cancellationToken);
        return Ok(new MonitoredGroupsResponseDto
        {
            Status = true,
            Descricao = linked > 0
                ? $"{linked} imagem(ns) do disco foram vinculadas."
                : "Nenhuma imagem pendente no disco."
        });
    }

    [HttpPost("resetar")]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> ResetAndResyncAsync(
        [FromQuery] string? channel = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _service.ResetAndResyncAsync(ResolveUserId(), channel, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao limpar grupos monitorados");
            return Ok(Fail(Describe(ex)));
        }
    }

    [HttpPost("{offerId:int}/validar")]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> ValidateAsync(
        int offerId,
        [FromQuery] string? channel = null,
        CancellationToken cancellationToken = default)
    {
        var userId = ResolveUserId();
        var result = await _service.ValidateAsync(userId, offerId, channel, cancellationToken);
        return result.Status ? Ok(result) : NotFound(result);
    }

    [HttpPost("{offerId:int}/clonar")]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> CloneAsync(
        int offerId,
        [FromQuery] string? channel = null,
        CancellationToken cancellationToken = default)
    {
        var userId = ResolveUserId();
        var result = await _service.CloneAsync(userId, offerId, channel, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{offerId:int}/ignorar")]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> IgnoreAsync(
        int offerId,
        [FromQuery] string? channel = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.SetIgnoredAsync(ResolveUserId(), offerId, ignored: true, channel, cancellationToken);
        return result.Status ? Ok(result) : NotFound(result);
    }

    [HttpPost("{offerId:int}/restaurar")]
    public async Task<ActionResult<MonitoredGroupsResponseDto>> RestoreAsync(
        int offerId,
        [FromQuery] string? channel = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.SetIgnoredAsync(ResolveUserId(), offerId, ignored: false, channel, cancellationToken);
        return result.Status ? Ok(result) : NotFound(result);
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

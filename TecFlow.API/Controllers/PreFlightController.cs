using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Infrastructure.Interfaces;

namespace TecFlow.API.Controllers;

[Authorize]
[ApiController]
[Route("api/pre-flight")]
public sealed class PreFlightController : ControllerBase
{
    private readonly IPreFlightService _preFlight;
    private readonly IUserContextProvider _userContext;

    public PreFlightController(IPreFlightService preFlight, IUserContextProvider userContext)
    {
        _preFlight = preFlight;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<ActionResult<PreFlightNotificationResponseDto>> ListAsync(CancellationToken cancellationToken) =>
        Ok(await _preFlight.ListAsync(ResolveUserId(), cancellationToken));

    [HttpPost("{id:int}/substituir")]
    public async Task<ActionResult<PreFlightNotificationResponseDto>> SubstituteAsync(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await _preFlight.SubstituteAsync(ResolveUserId(), id, cancellationToken));

    private int ResolveUserId()
    {
        var fromContext = _userContext.GetCurrentUserId();
        if (fromContext is > 0)
        {
            return fromContext.Value;
        }

        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out var userId) && userId > 0
            ? userId
            : 1;
    }
}

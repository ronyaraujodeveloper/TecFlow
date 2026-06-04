using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly INotificationHubService _notificationHub;

    public DevicesController(INotificationHubService notificationHub)
    {
        _notificationHub = notificationHub;
    }

    [HttpPost("register")]
    public async Task<ActionResult<DeviceRegisterResponseDto>> RegisterAsync(
        [FromBody] DeviceRegisterDto dto,
        CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _notificationHub.RegisterDeviceAsync(userId, dto, cancellationToken);

        if (!result.Status)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

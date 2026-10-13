using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.API.Security;
using TecFlow.Business.Dto;
using TecFlow.Business.Service.Master;

namespace TecFlow.API.Controllers.Master;

[ApiController]
[MasterScope]
[Authorize(Roles = MasterRoles.AnyMaster)]
public abstract class MasterControllerBase : ControllerBase
{
    protected MasterActor Actor
    {
        get
        {
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
            return new MasterActor(
                userId,
                User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                MasterRoles.Normalize(User.FindFirstValue(ClaimTypes.Role)),
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }
    }

    protected ActionResult<MasterOperationResultDto> Result(MasterOperationResultDto result) =>
        result.Success ? Ok(result) : BadRequest(new { status = false, descricao = result.Message });

    protected ActionResult Error(string message) => BadRequest(new { status = false, descricao = message });
}

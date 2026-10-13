using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Master;

namespace TecFlow.API.Controllers.Master;

/// <summary>Matriz de visibilidade da árvore de menus por plano SaaS.</summary>
[Route("api/master/menu-permissions")]
[Authorize(Policy = MasterPolicies.ManagePlans)]
public sealed class MasterMenuPermissionsController : MasterControllerBase
{
    private readonly IMenuPermissionService _menus;

    public MasterMenuPermissionsController(IMenuPermissionService menus)
    {
        _menus = menus;
    }

    [HttpGet]
    public async Task<ActionResult<MenuPermissionMatrixDto>> GetAsync(CancellationToken cancellationToken) =>
        Ok(await _menus.GetMatrixAsync(cancellationToken));

    [HttpPut]
    public async Task<ActionResult<MasterOperationResultDto>> SaveAsync([FromBody] MenuPermissionSaveDto request, CancellationToken cancellationToken) =>
        Result(await _menus.SaveMatrixAsync(Actor, request ?? new MenuPermissionSaveDto(), cancellationToken));
}

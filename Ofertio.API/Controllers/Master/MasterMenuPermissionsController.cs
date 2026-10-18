using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;

namespace Ofertio.API.Controllers.Master;

/// <summary>Matriz de visibilidade da árvore de menus por plano SaaS e reorganização (drag &amp; drop).</summary>
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

    [HttpPost("mover")]
    public async Task<ActionResult<MasterOperationResultDto>> MoveAsync([FromBody] MenuMoveDto request, CancellationToken cancellationToken) =>
        Result(await _menus.MoveAsync(Actor, request ?? new MenuMoveDto(), cancellationToken));

    [HttpPost("pastas")]
    public async Task<ActionResult<MasterOperationResultDto>> CreateFolderAsync([FromBody] MenuFolderCreateDto request, CancellationToken cancellationToken) =>
        Result(await _menus.CreateFolderAsync(Actor, request ?? new MenuFolderCreateDto(), cancellationToken));

    [HttpDelete("pastas/{id:int}")]
    public async Task<ActionResult<MasterOperationResultDto>> DeleteFolderAsync(int id, CancellationToken cancellationToken) =>
        Result(await _menus.DeleteFolderAsync(Actor, id, cancellationToken));
}

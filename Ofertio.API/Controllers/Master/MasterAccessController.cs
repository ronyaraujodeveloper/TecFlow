using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;

namespace Ofertio.API.Controllers.Master;

/// <summary>Operadores master, matriz RBAC, trilha de auditoria e emulação de inquilinos.</summary>
[Route("api/master")]
public sealed class MasterAccessController : MasterControllerBase
{
    private readonly IMasterOperatorService _operators;
    private readonly IMasterAuditService _audit;
    private readonly IImpersonationService _impersonation;

    public MasterAccessController(
        IMasterOperatorService operators,
        IMasterAuditService audit,
        IImpersonationService impersonation)
    {
        _operators = operators;
        _audit = audit;
        _impersonation = impersonation;
    }

    [HttpGet("me")]
    public ActionResult<object> Me()
    {
        var actor = Actor;
        return Ok(new
        {
            actor.UserId,
            actor.Email,
            actor.Role,
            RoleLabel = MasterRoles.Label(actor.Role),
            Permissions = MasterPermissionMatrix.PermissionsFor(actor.Role)
        });
    }

    [HttpGet("permissoes")]
    public ActionResult<MasterPermissionMatrixDto> Permissions() =>
        Ok(new MasterPermissionMatrixDto
        {
            Roles = MasterRoles.MasterRoleList
                .Select(role => new MasterRoleDto { Code = role, Label = MasterRoles.Label(role) })
                .ToList(),
            Permissions = MasterPermissions.All
                .Select(permission => new MasterPermissionRowDto
                {
                    Code = permission,
                    Label = MasterPermissions.Label(permission),
                    Roles = MasterPermissionMatrix.RolesFor(permission).ToList()
                })
                .ToList()
        });

    [HttpGet("operadores")]
    [Authorize(Policy = MasterPolicies.ManageOperators)]
    public async Task<ActionResult<IReadOnlyList<MasterOperatorDto>>> ListOperatorsAsync(CancellationToken cancellationToken) =>
        Ok(await _operators.ListAsync(cancellationToken));

    [HttpPost("operadores")]
    [Authorize(Policy = MasterPolicies.ManageOperators)]
    public async Task<ActionResult<MasterOperatorDto>> CreateOperatorAsync(
        [FromBody] MasterOperatorSaveDto request,
        CancellationToken cancellationToken)
    {
        var (item, error) = await _operators.CreateAsync(Actor, request, cancellationToken);
        return item is null ? Error(error ?? "Não foi possível criar o operador.") : Ok(item);
    }

    [HttpPut("operadores/{id:int}")]
    [Authorize(Policy = MasterPolicies.ManageOperators)]
    public async Task<ActionResult<MasterOperatorDto>> UpdateOperatorAsync(
        int id,
        [FromBody] MasterOperatorSaveDto request,
        CancellationToken cancellationToken)
    {
        var (item, error) = await _operators.UpdateAsync(Actor, id, request, cancellationToken);
        return item is null ? Error(error ?? "Não foi possível salvar o operador.") : Ok(item);
    }

    [HttpPost("operadores/{id:int}/ativo")]
    [Authorize(Policy = MasterPolicies.ManageOperators)]
    public async Task<ActionResult<MasterOperationResultDto>> SetOperatorActiveAsync(
        int id,
        [FromQuery] bool value,
        CancellationToken cancellationToken) =>
        Result(await _operators.SetActiveAsync(Actor, id, value, cancellationToken));

    [HttpGet("auditoria")]
    [Authorize(Policy = MasterPolicies.ViewAudit)]
    public async Task<ActionResult<IReadOnlyList<MasterAuditLogDto>>> AuditAsync(
        [FromQuery] MasterAuditQueryDto query,
        CancellationToken cancellationToken) =>
        Ok(await _audit.QueryAsync(query ?? new MasterAuditQueryDto(), cancellationToken));

    [HttpPost("emular/{tenantId:guid}")]
    [Authorize(Policy = MasterPolicies.Impersonate)]
    public async Task<ActionResult<ImpersonationResultDto>> StartImpersonationAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await _impersonation.StartAsync(Actor, tenantId, cancellationToken);
        return result.Success ? Ok(result) : Error(result.Message);
    }

    [HttpPost("emular/encerrar")]
    public async Task<ActionResult<MasterOperationResultDto>> StopImpersonationAsync(
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        await _impersonation.StopAsync(Actor, tenantId, cancellationToken);
        return Ok(new MasterOperationResultDto { Success = true, Message = "Emulação encerrada." });
    }
}

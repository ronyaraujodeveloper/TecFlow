using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Master;

namespace TecFlow.API.Controllers.Master;

/// <summary>Central 360° de inquilinos.</summary>
[Route("api/master/tenants")]
public sealed class MasterTenantsController : MasterControllerBase
{
    private readonly ITenantAdminService _tenants;

    public MasterTenantsController(ITenantAdminService tenants)
    {
        _tenants = tenants;
    }

    [HttpGet]
    [Authorize(Policy = MasterPolicies.ViewTenants)]
    public async Task<ActionResult<IReadOnlyList<TenantSummaryDto>>> ListAsync(
        [FromQuery] TenantQueryDto query,
        CancellationToken cancellationToken) =>
        Ok(await _tenants.ListAsync(query ?? new TenantQueryDto(), cancellationToken));

    [HttpPost("{tenantId:guid}/ciclo")]
    [Authorize(Policy = MasterPolicies.ManageTenants)]
    public async Task<ActionResult<MasterOperationResultDto>> LifecycleAsync(
        Guid tenantId,
        [FromBody] TenantLifecycleRequestDto request,
        CancellationToken cancellationToken) =>
        Result(await _tenants.ApplyLifecycleAsync(Actor, tenantId, request ?? new TenantLifecycleRequestDto(), cancellationToken));

    [HttpPut("{tenantId:guid}/plano")]
    [Authorize(Policy = MasterPolicies.ManageBilling)]
    public async Task<ActionResult<MasterOperationResultDto>> ChangePlanAsync(
        Guid tenantId,
        [FromBody] TenantPlanChangeDto request,
        CancellationToken cancellationToken) =>
        Result(await _tenants.ChangePlanAsync(Actor, tenantId, request ?? new TenantPlanChangeDto(), cancellationToken));

    [HttpPost("{tenantId:guid}/creditos")]
    [Authorize(Policy = MasterPolicies.AdjustCredits)]
    public async Task<ActionResult<MasterOperationResultDto>> AdjustCreditsAsync(
        Guid tenantId,
        [FromBody] TenantCreditAdjustDto request,
        CancellationToken cancellationToken) =>
        Result(await _tenants.AdjustCreditsAsync(Actor, tenantId, request ?? new TenantCreditAdjustDto(), cancellationToken));

    [HttpGet("{tenantId:guid}/armazenamento")]
    [Authorize(Policy = MasterPolicies.ViewTenants)]
    public async Task<ActionResult<TenantStorageDto>> StorageAsync(Guid tenantId, CancellationToken cancellationToken) =>
        Ok(await _tenants.GetStorageAsync(tenantId, cancellationToken));

    [HttpPost("{tenantId:guid}/armazenamento/purgar")]
    [Authorize(Policy = MasterPolicies.ManageTenants)]
    public async Task<ActionResult<MasterOperationResultDto>> PurgeAsync(Guid tenantId, CancellationToken cancellationToken) =>
        Result(await _tenants.PurgeMediaAsync(Actor, tenantId, cancellationToken));

    [HttpPost("{tenantId:guid}/assinatura")]
    [Authorize(Policy = MasterPolicies.ManageBilling)]
    public async Task<ActionResult<MasterOperationResultDto>> CreateSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken) =>
        Result(await _tenants.CreateGatewaySubscriptionAsync(Actor, tenantId, cancellationToken));
}

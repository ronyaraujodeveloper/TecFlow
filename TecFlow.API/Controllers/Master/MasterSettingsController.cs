using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Master;

namespace TecFlow.API.Controllers.Master;

/// <summary>Cofre de chaves globais e planos SaaS.</summary>
[Route("api/master")]
public sealed class MasterSettingsController : MasterControllerBase
{
    private readonly ISystemSettingsService _settings;
    private readonly ISaasPlanService _plans;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IMasterAuditService _audit;
    private readonly IGlobalCredentialHealthService _credentialHealth;

    public MasterSettingsController(
        ISystemSettingsService settings,
        ISaasPlanService plans,
        IHostApplicationLifetime lifetime,
        IMasterAuditService audit,
        IGlobalCredentialHealthService credentialHealth)
    {
        _settings = settings;
        _plans = plans;
        _lifetime = lifetime;
        _audit = audit;
        _credentialHealth = credentialHealth;
    }

    [HttpGet("configuracoes")]
    [Authorize(Policy = MasterPolicies.ManageGlobalKeys)]
    public async Task<ActionResult<IReadOnlyList<SystemSettingItemDto>>> ListSettingsAsync(CancellationToken cancellationToken) =>
        Ok(await _settings.ListAsync(cancellationToken));

    [HttpPut("configuracoes")]
    [Authorize(Policy = MasterPolicies.ManageGlobalKeys)]
    public async Task<ActionResult<MasterOperationResultDto>> SaveSettingsAsync(
        [FromBody] SystemSettingSaveDto request,
        CancellationToken cancellationToken) =>
        Result(await _settings.SaveAsync(Actor, request ?? new SystemSettingSaveDto(), cancellationToken));

    [HttpPost("configuracoes/saude-chaves")]
    [Authorize(Policy = MasterPolicies.ManageGlobalKeys)]
    public async Task<ActionResult<IReadOnlyList<DeveloperAccountCheckDto>>> CheckGlobalKeysAsync(CancellationToken cancellationToken) =>
        Ok(await _credentialHealth.CheckAsync(cancellationToken));

    /// <summary>Encerra o processo da API; o IIS recicla e recarrega todas as integrações com as novas chaves.</summary>
    [HttpPost("configuracoes/reiniciar-api")]
    [Authorize(Policy = MasterPolicies.ManageGlobalKeys)]
    public async Task<ActionResult<MasterOperationResultDto>> RestartApiAsync(CancellationToken cancellationToken)
    {
        await _audit.LogAsync(Actor, "api.reinicio", "Sistema", null, "Reinício da API solicitado após troca de chaves.", cancellationToken);
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            _lifetime.StopApplication();
        });
        return Ok(new MasterOperationResultDto
        {
            Success = true,
            Message = "API reiniciando. Aguarde alguns segundos e recarregue a página."
        });
    }

    [HttpGet("planos")]
    public async Task<ActionResult<IReadOnlyList<SaasPlanDto>>> ListPlansAsync(CancellationToken cancellationToken) =>
        Ok(await _plans.ListAsync(includeInactive: true, cancellationToken));

    [HttpPut("planos")]
    [Authorize(Policy = MasterPolicies.ManagePlans)]
    public async Task<ActionResult<SaasPlanDto>> SavePlanAsync([FromBody] SaasPlanDto request, CancellationToken cancellationToken)
    {
        var (item, error) = await _plans.SaveAsync(Actor, request ?? new SaasPlanDto(), cancellationToken);
        return item is null ? Error(error ?? "Não foi possível salvar o plano.") : Ok(item);
    }
}

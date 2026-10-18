using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;

namespace Ofertio.API.Controllers.Master;

/// <summary>Dashboard executivo e central de comunicação.</summary>
[Route("api/master")]
public sealed class MasterInsightsController : MasterControllerBase
{
    private readonly IMasterMetricsService _metrics;
    private readonly ISystemCommunicationService _communication;

    public MasterInsightsController(IMasterMetricsService metrics, ISystemCommunicationService communication)
    {
        _metrics = metrics;
        _communication = communication;
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = MasterPolicies.ViewMetrics)]
    public async Task<ActionResult<MasterDashboardDto>> DashboardAsync(CancellationToken cancellationToken) =>
        Ok(await _metrics.GetDashboardAsync(cancellationToken));

    [HttpGet("avisos")]
    [Authorize(Policy = MasterPolicies.ManageCommunication)]
    public async Task<ActionResult<IReadOnlyList<SystemBannerDto>>> ListBannersAsync(CancellationToken cancellationToken) =>
        Ok(await _communication.ListBannersAsync(onlyActive: false, cancellationToken));

    [HttpPut("avisos")]
    [Authorize(Policy = MasterPolicies.ManageCommunication)]
    public async Task<ActionResult<SystemBannerDto>> SaveBannerAsync([FromBody] SystemBannerDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Title) || string.IsNullOrWhiteSpace(request.Message))
        {
            return Error("Informe título e mensagem do aviso.");
        }

        return Ok(await _communication.SaveBannerAsync(Actor, request, cancellationToken));
    }

    [HttpDelete("avisos/{id:int}")]
    [Authorize(Policy = MasterPolicies.ManageCommunication)]
    public async Task<ActionResult<MasterOperationResultDto>> DeleteBannerAsync(int id, CancellationToken cancellationToken) =>
        Result(await _communication.DeleteBannerAsync(Actor, id, cancellationToken)
            ? new MasterOperationResultDto { Success = true, Message = "Aviso excluído." }
            : new MasterOperationResultDto { Success = false, Message = "Aviso não encontrado." });

    [HttpPost("notificacoes")]
    [Authorize(Policy = MasterPolicies.ManageCommunication)]
    public async Task<ActionResult<MasterOperationResultDto>> NotifyAsync(
        [FromBody] SegmentNotificationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Title) || string.IsNullOrWhiteSpace(request.Message))
        {
            return Error("Informe título e mensagem da notificação.");
        }

        var count = await _communication.NotifySegmentAsync(Actor, request, cancellationToken);
        return Ok(new MasterOperationResultDto { Success = true, Message = $"Notificação enviada para {count} inquilino(s)." });
    }

    [HttpGet("changelog")]
    [Authorize(Policy = MasterPolicies.ManageCommunication)]
    public async Task<ActionResult<IReadOnlyList<ProductChangelogDto>>> ListChangelogAsync(CancellationToken cancellationToken) =>
        Ok(await _communication.ListChangelogAsync(onlyPublished: false, cancellationToken));

    [HttpPut("changelog")]
    [Authorize(Policy = MasterPolicies.ManageCommunication)]
    public async Task<ActionResult<ProductChangelogDto>> SaveChangelogAsync([FromBody] ProductChangelogDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Version) || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
        {
            return Error("Informe versão, título e descrição da novidade.");
        }

        return Ok(await _communication.SaveChangelogAsync(Actor, request, cancellationToken));
    }

    [HttpDelete("changelog/{id:int}")]
    [Authorize(Policy = MasterPolicies.ManageCommunication)]
    public async Task<ActionResult<MasterOperationResultDto>> DeleteChangelogAsync(int id, CancellationToken cancellationToken) =>
        Result(await _communication.DeleteChangelogAsync(Actor, id, cancellationToken)
            ? new MasterOperationResultDto { Success = true, Message = "Novidade excluída." }
            : new MasterOperationResultDto { Success = false, Message = "Novidade não encontrada." });
}

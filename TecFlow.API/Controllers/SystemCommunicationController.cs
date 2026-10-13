using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Security;

namespace TecFlow.API.Controllers;

/// <summary>Avisos globais, notificações do inquilino e novidades do produto (visão do usuário).</summary>
[Authorize]
[ApiController]
[Route("api/sistema")]
public sealed class SystemCommunicationController : ControllerBase
{
    private readonly ISystemCommunicationService _communication;

    public SystemCommunicationController(ISystemCommunicationService communication)
    {
        _communication = communication;
    }

    [HttpGet("avisos")]
    public async Task<ActionResult<IReadOnlyList<SystemBannerDto>>> BannersAsync(CancellationToken cancellationToken) =>
        Ok(await _communication.ListBannersAsync(onlyActive: true, cancellationToken));

    [HttpGet("notificacoes")]
    public async Task<ActionResult<IReadOnlyList<TenantNotificationDto>>> NotificationsAsync(CancellationToken cancellationToken) =>
        TryTenant(out var tenantId)
            ? Ok(await _communication.ListNotificationsAsync(tenantId, cancellationToken))
            : Ok(Array.Empty<TenantNotificationDto>());

    [HttpPost("notificacoes/{id:int}/lida")]
    public async Task<ActionResult<MasterOperationResultDto>> MarkReadAsync(int id, CancellationToken cancellationToken)
    {
        var ok = TryTenant(out var tenantId) && await _communication.MarkNotificationReadAsync(tenantId, id, cancellationToken);
        return Ok(new MasterOperationResultDto { Success = ok, Message = ok ? "Notificação lida." : "Notificação não encontrada." });
    }

    [HttpGet("novidades")]
    public async Task<ActionResult<IReadOnlyList<ProductChangelogDto>>> ChangelogAsync(CancellationToken cancellationToken) =>
        Ok(await _communication.ListChangelogAsync(onlyPublished: true, cancellationToken));

    private bool TryTenant(out Guid tenantId) =>
        Guid.TryParse(User.FindFirstValue(TecFlowClaimTypes.TenantId), out tenantId) && tenantId != Guid.Empty;
}

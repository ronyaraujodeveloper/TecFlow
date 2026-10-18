using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Security;

namespace Ofertio.API.Controllers;

/// <summary>Avisos globais, notificações do inquilino e novidades do produto (visão do usuário).</summary>
[Authorize]
[ApiController]
[Route("api/sistema")]
public sealed class SystemCommunicationController : ControllerBase
{
    private readonly ISystemCommunicationService _communication;
    private readonly IMenuPermissionService _menus;

    public SystemCommunicationController(ISystemCommunicationService communication, IMenuPermissionService menus)
    {
        _communication = communication;
        _menus = menus;
    }

    /// <summary>Sidebar do usuário logado conforme o plano contratado pelo inquilino.</summary>
    /// <param name="visao">"cliente" quando um master está na visão de cliente (vê o que os clientes veem).</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    [HttpGet("menu")]
    public async Task<ActionResult<IReadOnlyList<ClientMenuNodeDto>>> MenuAsync([FromQuery] string? visao, CancellationToken cancellationToken)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var clientView = string.Equals(visao, "cliente", StringComparison.OrdinalIgnoreCase);
        return Ok(await _menus.GetClientMenuAsync(userId, clientView, cancellationToken));
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
        Guid.TryParse(User.FindFirstValue(OfertioClaimTypes.TenantId), out tenantId) && tenantId != Guid.Empty;
}

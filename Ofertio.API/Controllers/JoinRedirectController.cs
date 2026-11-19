using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;

namespace Ofertio.API.Controllers;

/// <summary>
/// Botão "entrar no grupo" da página pública: /j/{id}. Conta o clique e manda para o convite cadastrado.
/// Só redireciona para o link já validado do afiliado; nada vem da URL da chamada, então não vira redirecionador aberto.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class JoinRedirectController : ControllerBase
{
    private readonly IPublicPageProfileService _profiles;
    private readonly IPublicPageEventService _events;
    private readonly ILinkAvailabilityService _availability;

    public JoinRedirectController(
        IPublicPageProfileService profiles,
        IPublicPageEventService events,
        ILinkAvailabilityService availability)
    {
        _profiles = profiles;
        _events = events;
        _availability = availability;
    }

    [HttpGet("/j/{id:int}")]
    public async Task<IActionResult> RedirectAsync(int id, CancellationToken cancellationToken)
    {
        var target = await _profiles.ResolveJoinTargetAsync(id, cancellationToken);
        if (target is null)
        {
            return Notice(StatusCodes.Status404NotFound, "Este convite não está mais disponível", "O link de entrada no grupo foi desativado ou removido.");
        }

        var availability = await _availability.CheckAsync(target.TenantId, cancellationToken);
        if (!availability.IsServing)
        {
            return Notice(StatusCodes.Status410Gone, LinkAvailabilityRules.UnavailableLinkTitle, LinkAvailabilityRules.UnavailableLinkMessage);
        }

        await _events.RecordAsync(
            target.TenantId,
            target.UserId,
            PublicPageEvent.KindJoin,
            ClientRequestInfo.Read(HttpContext),
            id,
            target.Channel,
            cancellationToken);
        return Redirect(target.InviteUrl);
    }

    private static ContentResult Notice(int status, string title, string message) => new()
    {
        StatusCode = status,
        ContentType = "text/html; charset=utf-8",
        Content = "<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">"
            + "<meta name=\"robots\" content=\"noindex\"><title>" + System.Net.WebUtility.HtmlEncode(title) + "</title>"
            + "<style>body{font-family:system-ui,Segoe UI,Arial,sans-serif;background:#f8fafc;color:#0f172a;display:flex;min-height:100vh;align-items:center;justify-content:center;margin:0;padding:1rem}"
            + "main{max-width:28rem;text-align:center}h1{font-size:1.4rem;margin:0 0 .75rem}p{color:#475569;line-height:1.5}</style></head>"
            + "<body><main><h1>" + System.Net.WebUtility.HtmlEncode(title) + "</h1><p>" + System.Net.WebUtility.HtmlEncode(message) + "</p></main></body></html>"
    };
}

using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Attribution;
using Ofertio.Core.Security;

namespace Ofertio.API.Controllers;

/// <summary>Números do Painel do cliente: cliques por período, origem, grupo e produto.</summary>
[ApiController]
[Authorize]
[Route("api/painel")]
public sealed class PainelController : ControllerBase
{
    private readonly ILinkAnalyticsService _analytics;

    public PainelController(ILinkAnalyticsService analytics)
    {
        _analytics = analytics;
    }

    /// <param name="from">Data inicial (yyyy-MM-dd), horário de Brasília.</param>
    /// <param name="to">Data final (yyyy-MM-dd), inclusive.</param>
    /// <param name="grain">dia, semana, mês ou ano. Sem valor, o sistema sugere pelo tamanho do período.</param>
    /// <param name="shopId">Opcional: só uma loja.</param>
    [HttpGet("visao-geral")]
    public async Task<ActionResult<AnalyticsOverviewDto>> GetOverviewAsync(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? grain,
        [FromQuery] string? shopId,
        CancellationToken cancellationToken)
    {
        var request = BuildRequest(from, to, grain, shopId, out var error);
        if (request is null)
        {
            return error is null ? Unauthorized(new { message = "Usuário não autenticado." }) : BadRequest(new { message = error });
        }

        var (data, serviceError) = await _analytics.GetOverviewAsync(request, cancellationToken);
        return data is null ? BadRequest(new { message = serviceError }) : Ok(data);
    }

    [HttpGet("produtos/{linkGroupId:guid}")]
    public async Task<ActionResult<AnalyticsProductDetailDto>> GetProductAsync(
        Guid linkGroupId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? grain,
        [FromQuery] string? shopId,
        CancellationToken cancellationToken)
    {
        var request = BuildRequest(from, to, grain, shopId, out var error);
        if (request is null)
        {
            return error is null ? Unauthorized(new { message = "Usuário não autenticado." }) : BadRequest(new { message = error });
        }

        var (data, serviceError) = await _analytics.GetProductAsync(request, linkGroupId, cancellationToken);
        return data is null ? NotFound(new { message = serviceError }) : Ok(data);
    }

    private AnalyticsRequest? BuildRequest(string? from, string? to, string? grain, string? shopId, out string? error)
    {
        error = null;
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Guid.TryParse(User.FindFirstValue(OfertioClaimTypes.TenantId), out var tenantId))
        {
            return null;
        }

        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate)
            || !DateOnly.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
        {
            error = "Informe as datas no formato aaaa-mm-dd.";
            return null;
        }

        AnalyticsGrain parsedGrain;
        if (string.IsNullOrWhiteSpace(grain))
        {
            parsedGrain = AnalyticsPeriodRules.SuggestGrain(fromDate, toDate);
        }
        else if (!AnalyticsPeriodRules.TryParseGrain(grain, out parsedGrain))
        {
            error = "Agrupamento inválido. Use dia, semana, mês ou ano.";
            return null;
        }

        return new AnalyticsRequest(tenantId, userId, fromDate, toDate, parsedGrain, shopId);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Database.Filter;
using TecFlow.Database.Pagin;

namespace TecFlow.Orquestrador.Controllers;

[ApiController]
[Route("api/afiliados/analytics")]
[Authorize]
public class AffiliateAnalyticsController : ControllerBase
{
    private readonly IAffiliateAnalyticsService _analyticsService;

    public AffiliateAnalyticsController(IAffiliateAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("conciliacao")]
    public async Task<ActionResult<AffiliateReconciliationResponseDto>> GetReconciliationAsync(
        [FromQuery] AffiliateReconciliationFilter filter,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new AffiliateReconciliationResponseDto
            {
                Status = false,
                Descricao = "Não autorizado."
            });
        }

        var affiliateId = filter.AffiliateId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(affiliateId))
        {
            return BadRequest(new AffiliateReconciliationResponseDto
            {
                Status = false,
                Descricao = "AffiliateId é obrigatório (código do afiliado)."
            });
        }

        var start = filter.StartDate ?? DateTime.UtcNow.AddDays(-30);
        var end = filter.EndDate ?? DateTime.UtcNow;

        try
        {
            var (performance, discrepancies) = await _analyticsService.ReconcileCommissionsAsync(
                userId,
                affiliateId,
                start,
                end,
                cancellationToken);

            var (items, meta) = PagedListHelper.Slice(discrepancies, filter);

            return Ok(new AffiliateReconciliationResponseDto
            {
                Status = true,
                Descricao = "Conciliação concluída.",
                Performance = performance,
                Discrepancies = items,
                Paging = PagingInfoDto.FromMeta(meta)
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new AffiliateReconciliationResponseDto
            {
                Status = false,
                Descricao = ex.Message
            });
        }
    }

    [HttpGet("comissoes-marketplace")]
    public async Task<ActionResult<AffiliateReconciliationResponseDto>> GetMarketplaceCommissionsAsync(
        [FromQuery] AffiliateReconciliationFilter filter,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new AffiliateReconciliationResponseDto { Status = false, Descricao = "Não autorizado." });
        }

        if (string.IsNullOrWhiteSpace(filter.AffiliateId))
        {
            return BadRequest(new AffiliateReconciliationResponseDto
            {
                Status = false,
                Descricao = "AffiliateId é obrigatório."
            });
        }

        var start = filter.StartDate ?? DateTime.UtcNow.AddDays(-30);
        var end = filter.EndDate ?? DateTime.UtcNow;

        var lines = await _analyticsService.FetchMarketplaceCommissionsAsync(
            userId,
            filter.AffiliateId,
            start,
            end,
            cancellationToken);

        return Ok(new AffiliateReconciliationResponseDto
        {
            Status = true,
            Descricao = $"{lines.Count} linhas importadas do marketplace.",
            Performance = new AffiliatePerformanceDto
            {
                AffiliateId = filter.AffiliateId,
                OwnerId = userId,
                PaidCommission = lines.Sum(l => l.PaidCommission),
                TotalConversions = lines.Sum(l => l.Conversions),
                TotalClicks = lines.Sum(l => (long)l.Clicks),
                PeriodStartUtc = start,
                PeriodEndUtc = end
            }
        });
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out userId);
    }
}

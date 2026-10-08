using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Infrastructure.Interfaces;

namespace TecFlow.API.Controllers;

[Authorize]
[ApiController]
[Route("api/achadinhos")]
public sealed class DealIntelligenceController : ControllerBase
{
    private readonly IDealCreditsService _credits;
    private readonly IUserContextProvider _userContext;

    public DealIntelligenceController(IDealCreditsService credits, IUserContextProvider userContext)
    {
        _credits = credits;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<ActionResult<DealCreditsResponseDto>> ListAsync(
        [FromQuery] string? q = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] bool? coupon = null,
        [FromQuery] string? store = null,
        CancellationToken cancellationToken = default)
    {
        var search = new LiveSearchFilterDto
        {
            Keyword = q,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            HasCoupon = coupon,
            Store = store
        };
        return Ok(await _credits.ListShowcaseAsync(
            ResolveUserId(),
            search.HasAny ? search : null,
            cancellationToken));
    }

    [HttpPost("{id:int}/desbloquear")]
    public async Task<ActionResult<DealCreditsResponseDto>> UnlockAsync(int id, CancellationToken cancellationToken) =>
        Ok(await _credits.UnlockAsync(ResolveUserId(), id, cancellationToken));

    [HttpPost("creditos/pacote")]
    public async Task<ActionResult<DealCreditsResponseDto>> TopUpAsync(
        [FromBody] TopUpCreditsRequestDto? request,
        CancellationToken cancellationToken) =>
        Ok(await _credits.TopUpAsync(ResolveUserId(), request?.PackSize ?? 0, cancellationToken));

    private int ResolveUserId()
    {
        var fromContext = _userContext.GetCurrentUserId();
        if (fromContext is > 0)
        {
            return fromContext.Value;
        }

        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out var userId) && userId > 0
            ? userId
            : 1;
    }
}

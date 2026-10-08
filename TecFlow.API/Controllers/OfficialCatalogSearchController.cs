using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Database.Filter;
using TecFlow.Infrastructure.Interfaces;

namespace TecFlow.API.Controllers;

[Authorize]
[ApiController]
[Route("api/busca")]
public sealed class OfficialCatalogSearchController : ControllerBase
{
    private readonly IOfficialCatalogSearchService _search;
    private readonly IUserContextProvider _userContext;

    public OfficialCatalogSearchController(
        IOfficialCatalogSearchService search,
        IUserContextProvider userContext)
    {
        _search = search;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<ActionResult<OfficialCatalogSearchResponseDto>> SearchAsync(
        [FromQuery] OfficialCatalogSearchFilter filter,
        CancellationToken cancellationToken = default) =>
        Ok(await _search.SearchAsync(ResolveUserId(), filter, cancellationToken));

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

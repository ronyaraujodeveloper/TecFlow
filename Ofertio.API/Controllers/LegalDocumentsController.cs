using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Legal;
using Ofertio.Database.Filter;

namespace Ofertio.API.Controllers;

[ApiController]
[Authorize]
[Route("api/legal")]
public class LegalDocumentsController : ControllerBase
{
    private readonly ITermsDocumentService _terms;

    public LegalDocumentsController(ITermsDocumentService terms)
    {
        _terms = terms;
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<TermsVersionResponseDto>> GetActiveAsync(CancellationToken cancellationToken)
    {
        var result = await _terms.GetActiveAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("acceptance")]
    public async Task<ActionResult<UserTermsAcceptanceResponseDto>> GetAcceptanceAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _terms.GetStatusAsync(userId.Value, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpGet("acceptances")]
    public async Task<ActionResult<UserTermsAcceptanceResponseDto>> ListMineAsync(
        [FromQuery] UserTermsAcceptanceFilter filter,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        filter.UserId = userId.Value;
        var result = await _terms.ListMineAsync(filter, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPost("acceptance")]
    public async Task<ActionResult<UserTermsAcceptanceResponseDto>> AcceptAsync(
        [FromBody] UserTermsAcceptanceDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(Fail("Usuário não autenticado."));
        }

        var result = await _terms.AcceptAsync(userId.Value, request, ClientIp(), cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    private string? ClientIp() => TermsDocumentRules.ResolveClientIp(
        Request.Headers["CF-Connecting-IP"].FirstOrDefault(),
        Request.Headers["X-Forwarded-For"].FirstOrDefault(),
        HttpContext.Connection.RemoteIpAddress?.ToString());

    private int? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimValue, out var userId) ? userId : null;
    }

    private static UserTermsAcceptanceResponseDto Fail(string message) =>
        new() { Status = false, Descricao = message };
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Database.Filter;

namespace Ofertio.API.Controllers.Master;

[Route("api/master/termos")]
[Authorize(Policy = MasterPolicies.ManageLegal)]
public class MasterTermsController : MasterControllerBase
{
    private readonly ITermsDocumentService _terms;
    private readonly IMasterAuditService _audit;

    public MasterTermsController(ITermsDocumentService terms, IMasterAuditService audit)
    {
        _terms = terms;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<TermsVersionResponseDto>> ListAsync(
        [FromQuery] TermsVersionFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _terms.ListAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TermsVersionResponseDto>> PublishAsync(
        [FromBody] TermsVersionDto request,
        CancellationToken cancellationToken)
    {
        var result = await _terms.PublishAsync(request, cancellationToken);
        if (!result.Status || result.Data is null)
        {
            return BadRequest(result);
        }

        await _audit.LogAsync(
            Actor,
            "TermsPublished",
            "TermsVersion",
            result.Data.Id.ToString(),
            $"{result.Data.DocumentKind} {result.Data.Version}",
            cancellationToken);
        return Ok(result);
    }
}

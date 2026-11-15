using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;

namespace Ofertio.API.Controllers;

/// <summary>Planos e regras do teste grátis para a página inicial, sem exigir login e sem dados internos.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/plans")]
public sealed class PublicPlansController : ControllerBase
{
    private readonly ISaasPlanService _plans;

    public PublicPlansController(ISaasPlanService plans)
    {
        _plans = plans;
    }

    [HttpGet]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<PublicPlansDto>> GetAsync(CancellationToken cancellationToken) =>
        Ok(await _plans.GetPublicAsync(cancellationToken));
}
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;

namespace Ofertio.API.Controllers;

/// <summary>Meus dados e Meu plano do usuário autenticado.</summary>
[ApiController]
[Authorize]
[Route("api/conta")]
public sealed class MyAccountController : ControllerBase
{
    private readonly IUserProfileService _profiles;
    private readonly IMyPlanService _plans;

    public MyAccountController(IUserProfileService profiles, IMyPlanService plans)
    {
        _profiles = profiles;
        _plans = plans;
    }

    [HttpGet("dados")]
    public async Task<ActionResult<UserProfileResponseDto>> GetProfileAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new UserProfileResponseDto { Status = false, Descricao = "Usuário não autenticado." });
        }

        var result = await _profiles.GetAsync(userId, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPut("dados")]
    public async Task<ActionResult<UserProfileResponseDto>> SaveProfileAsync(
        [FromBody] UserProfileDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new UserProfileResponseDto { Status = false, Descricao = "Usuário não autenticado." });
        }

        var result = await _profiles.SaveAsync(userId, request ?? new UserProfileDto(), cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpGet("plano")]
    public async Task<ActionResult<MyPlanResponseDto>> GetPlanAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new MyPlanResponseDto { Status = false, Descricao = "Usuário não autenticado." });
        }

        var result = await _plans.GetOverviewAsync(userId, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPost("plano/pagamento")]
    public async Task<ActionResult<PlanCheckoutResponseDto>> CheckoutAsync(
        [FromBody] PlanCheckoutDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new PlanCheckoutResponseDto { Status = false, Descricao = "Usuário não autenticado." });
        }

        var result = await _plans.CheckoutAsync(userId, request ?? new PlanCheckoutDto(), cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpGet("plano/pagamento/{id:int}")]
    public async Task<ActionResult<PlanCheckoutResponseDto>> GetCheckoutAsync(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new PlanCheckoutResponseDto { Status = false, Descricao = "Usuário não autenticado." });
        }

        var result = await _plans.GetCheckoutAsync(userId, id, cancellationToken);
        return result.Status ? Ok(result) : NotFound(result);
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out userId);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Core.Enums;

namespace TecFlow.API.Controllers;

[ApiController]
[Route("api/marketplace-auth")]
public class MarketplaceAuthController : ControllerBase
{
    private readonly IMarketplaceAuthService _marketplaceAuthService;

    public MarketplaceAuthController(IMarketplaceAuthService marketplaceAuthService)
    {
        _marketplaceAuthService = marketplaceAuthService;
    }

    /// <summary>Gera URL oficial de autorização OAuth para TikTok Shop ou Shopee.</summary>
    [HttpGet("authorize-url")]
    [AllowAnonymous]
    public ActionResult<object> GetAuthorizationUrl(
        [FromQuery] MarketplaceType type,
        [FromQuery] string redirectUri,
        [FromQuery] string? state = null)
    {
        var url = _marketplaceAuthService.GenerateAuthorizationUrl(type, redirectUri, state);
        return Ok(new { authorizationUrl = url, marketplace = type.ToString() });
    }

    /// <summary>Callback OAuth: troca o authorization code por tokens e persiste no banco.</summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<ActionResult<MarketplaceTokenResult>> CallbackAsync(
        [FromQuery] MarketplaceType type,
        [FromQuery] string code,
        [FromQuery] string shopId,
        CancellationToken cancellationToken)
    {
        var result = await _marketplaceAuthService.CallbackAndGenerateTokensAsync(
            type, code, shopId, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>Retorna access token válido (renova automaticamente se expirado).</summary>
    [HttpGet("token")]
    [Authorize]
    public async Task<ActionResult<object>> GetValidTokenAsync(
        [FromQuery] string shopId,
        [FromQuery] MarketplaceType type,
        CancellationToken cancellationToken)
    {
        var token = await _marketplaceAuthService.GetValidTokenAsync(shopId, type, cancellationToken);
        return Ok(new { shopId, marketplace = type.ToString(), accessToken = token });
    }
}

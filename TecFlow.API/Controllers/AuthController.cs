using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto.Auth;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IPlatformAuthService _platformAuthService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IPlatformAuthService platformAuthService,
        ILogger<AuthController> logger)
    {
        _platformAuthService = platformAuthService;
        _logger = logger;
    }

    [HttpPost("tiktok/login")]
    [AllowAnonymous]
    public Task<IActionResult> TikTokLogin([FromBody] PlatformAuthDto request, CancellationToken cancellationToken)
        => LoginForPlatformAsync("TikTok", request, cancellationToken);

    [HttpPost("shopee/login")]
    [AllowAnonymous]
    public Task<IActionResult> ShopeeLogin([FromBody] PlatformAuthDto request, CancellationToken cancellationToken)
        => LoginForPlatformAsync("Shopee", request, cancellationToken);

    [HttpPost("providers/vincular")]
    [Authorize]
    public async Task<ActionResult<AuthProviderResponseDto>> LinkProviderAsync(
        [FromBody] LinkProviderDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new AuthProviderResponseDto
            {
                Status = false,
                Descricao = "Usuário não autenticado."
            });
        }

        var result = await _platformAuthService.LinkProviderAsync(userId.Value, request, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("providers/desvincular")]
    [Authorize]
    public async Task<ActionResult<AuthProviderResponseDto>> UnlinkProviderAsync(
        [FromQuery] string provider,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new AuthProviderResponseDto
            {
                Status = false,
                Descricao = "Usuário não autenticado."
            });
        }

        var result = await _platformAuthService.UnlinkProviderAsync(userId.Value, provider, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<ActionResult<AuthProviderResponseDto>> ChangePasswordAsync(
        [FromBody] ChangePasswordDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new AuthProviderResponseDto
            {
                Status = false,
                Descricao = "Usuário não autenticado."
            });
        }

        var result = await _platformAuthService.ChangePasswordAsync(userId.Value, request, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    private async Task<IActionResult> LoginForPlatformAsync(
        string platform,
        PlatformAuthDto request,
        CancellationToken cancellationToken)
    {
        var result = await _platformAuthService.LoginAsync(platform, request, cancellationToken);
        if (!result.Success)
        {
            _logger.LogWarning(
                "Falha de login para {Platform}. Code={ErrorCode}",
                platform,
                result.ErrorCode);

            return result.ErrorCode switch
            {
                "INVALID_CREDENTIALS" or "SOCIAL_TOKEN_INVALID" => Unauthorized(new
                {
                    Message = result.ErrorMessage,
                    ErrorCode = result.ErrorCode
                }),
                _ => BadRequest(new
                {
                    Message = result.ErrorMessage,
                    ErrorCode = result.ErrorCode
                })
            };
        }

        return Ok(result.Token);
    }

    private int? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimValue, out var userId) ? userId : null;
    }
}

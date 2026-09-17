using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
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

        return await ExecuteProviderAsync(
            () => _platformAuthService.LinkProviderAsync(userId.Value, request, cancellationToken),
            "vincular provedor");
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

        return await ExecuteProviderAsync(
            () => _platformAuthService.UnlinkProviderAsync(userId.Value, provider, cancellationToken),
            "desvincular provedor");
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

        return await ExecuteProviderAsync(
            () => _platformAuthService.ChangePasswordAsync(userId.Value, request, cancellationToken),
            "alterar senha");
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponseDto>> RegisterAsync(
        [FromBody] UserDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _platformAuthService.RegisterAsync(request, cancellationToken);
            return result.Status ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado no registro de usuario.");
            throw;
        }
    }

    [HttpGet("status")]
    [HttpGet("providers/status")]
    [Authorize]
    public async Task<ActionResult<AuthProviderResponseDto>> GetProviderStatusAsync(
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

        return await ExecuteProviderAsync(
            () => _platformAuthService.GetProviderStatusAsync(userId.Value, cancellationToken),
            "status de provedores");
    }

    private async Task<ActionResult<AuthProviderResponseDto>> ExecuteProviderAsync(
        Func<Task<AuthProviderResponseDto>> action,
        string operation)
    {
        try
        {
            var result = await action();
            return result.Status ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao {Operation}.", operation);
            return StatusCode(StatusCodes.Status500InternalServerError, new AuthProviderResponseDto
            {
                Status = false,
                Descricao = "Não foi possível concluir a operação de segurança."
            });
        }
    }

    private async Task<IActionResult> LoginForPlatformAsync(
        string platform,
        PlatformAuthDto request,
        CancellationToken cancellationToken)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro inesperado no login da plataforma {Platform} via provedor {Provider}.",
                platform,
                request.Provider);
            throw;
        }
    }

    private int? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimValue, out var userId) ? userId : null;
    }
}

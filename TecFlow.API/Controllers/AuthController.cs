using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto.Auth;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Infrastructure.Security;
using TecFlow.Util.Validation;

namespace TecFlow.API.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserAccountRepository usuarioRepository,
        JwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _userAccountRepository = usuarioRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    [HttpPost("tiktok/login")]
    public Task<IActionResult> TikTokLogin([FromBody] PlatformAuthDto request, CancellationToken cancellationToken)
        => LoginForPlatformAsync("TikTok", request, cancellationToken);

    [HttpPost("shopee/login")]
    public Task<IActionResult> ShopeeLogin([FromBody] PlatformAuthDto request, CancellationToken cancellationToken)
        => LoginForPlatformAsync("Shopee", request, cancellationToken);

    private async Task<IActionResult> LoginForPlatformAsync(
        string platform,
        PlatformAuthDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            return BadRequest(new { Message = "Provedor de autenticação é obrigatório.", ErrorCode = "PROVIDER_REQUIRED" });
        }

        var provider = request.Provider.Trim();

        if (string.Equals(provider, "EmailPassword", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { Message = "E-mail e senha são obrigatórios.", ErrorCode = "CREDENTIALS_REQUIRED" });
            }

            var email = request.Email.Trim();

            if (!ValidationHelper.IsValidEmail(email))
            {
                return BadRequest(new { Message = "E-mail inválido.", ErrorCode = "INVALID_EMAIL" });
            }

            var usuario = await _userAccountRepository.GetByEmailAsync(email);
            if (usuario is null)
            {
                return Unauthorized(new { Message = "Credenciais inválidas.", ErrorCode = "INVALID_CREDENTIALS" });
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
            {
                return Unauthorized(new { Message = "Credenciais inválidas.", ErrorCode = "INVALID_CREDENTIALS" });
            }

            var token = _jwtTokenService.GenerateToken(usuario);
            return Ok(new AuthTokenDto
            {
                Token = token,
                UserId = usuario.Id.ToString(),
                DisplayName = usuario.Name,
                Platform = platform,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(8)
            });
        }

        if (string.IsNullOrWhiteSpace(request.AccessToken))
        {
            return BadRequest(new
            {
                Message = $"Token do provedor {provider} é obrigatório para login social.",
                ErrorCode = "SOCIAL_TOKEN_REQUIRED"
            });
        }

        _logger.LogInformation(
            "Login social recebido para {Platform} via {Provider}. Validação do token no provedor será implementada.",
            platform,
            provider);

        return Ok(new AuthTokenDto
        {
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            Platform = platform,
            DisplayName = $"{platform} ({provider})",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            UserId = "pending-social-validation"
        });
    }
}

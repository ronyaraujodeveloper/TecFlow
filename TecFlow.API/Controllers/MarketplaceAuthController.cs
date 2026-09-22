using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;
using TecFlow.Database.Filter;
using TecFlow.Infrastructure.Services.Integrations.Auth;

namespace TecFlow.API.Controllers;

[ApiController]
[Route("api/marketplace-auth")]
public class MarketplaceAuthController : ControllerBase
{
    private readonly IMarketplaceAuthService _marketplaceAuthService;
    private readonly IIntegracaoLojaService _integracaoLojaService;
    private readonly ILogger<MarketplaceAuthController> _logger;
    private readonly IHostEnvironment _hostEnvironment;

    /// <summary>
    /// Não injeta AppDbContext/TecFlowDbContext: o EF fica no serviço (DI scoped).
    /// O construtor só recebe abstrações e não dispara I/O nem consulta SQL.
    /// </summary>
    public MarketplaceAuthController(
        IMarketplaceAuthService marketplaceAuthService,
        IIntegracaoLojaService integracaoLojaService,
        ILogger<MarketplaceAuthController> logger,
        IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(marketplaceAuthService);
        ArgumentNullException.ThrowIfNull(integracaoLojaService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        _marketplaceAuthService = marketplaceAuthService;
        _integracaoLojaService = integracaoLojaService;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
    }

    /// <summary>Gera URL oficial de autorização OAuth para TikTok Shop ou Shopee.</summary>
    [HttpGet("authorize-url")]
    [AllowAnonymous]
    public ActionResult<MarketplaceAuthorizeUrlResponseDto> GetAuthorizationUrl(
        [FromQuery] MarketplaceType type,
        [FromQuery] string redirectUri,
        [FromQuery] string? state = null)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return BadRequest(new { error = "redirectUri é obrigatório." });
        }

        return GenerateAuthorizeResult(type, redirectUri, state);
    }

    /// <summary>Gera URL OAuth por slug da plataforma (shopee / tiktok).</summary>
    [HttpGet("{plataforma}/authorize-url")]
    [AllowAnonymous]
    public ActionResult<MarketplaceAuthorizeUrlResponseDto> GetPlatformAuthorizationUrl(
        string plataforma,
        [FromQuery] string? redirectUri,
        [FromQuery] string? state = null,
        [FromQuery] string? friendlyName = null,
        [FromQuery] string? lojaId = null)
    {
        if (!MarketplacePlatformRoute.TryParse(plataforma, out var type))
        {
            return BadRequest(new { error = "Plataforma inválida. Use shopee ou tiktok." });
        }

        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return BadRequest(new { error = "redirectUri é obrigatório." });
        }

        var stateValue = string.IsNullOrWhiteSpace(state) ? friendlyName ?? lojaId : state;
        return GenerateAuthorizeResult(type, redirectUri, stateValue);
    }

    private ActionResult<MarketplaceAuthorizeUrlResponseDto> GenerateAuthorizeResult(
        MarketplaceType type,
        string redirectUri,
        string? state)
    {
        try
        {
            var url = _marketplaceAuthService.GenerateAuthorizationUrl(type, redirectUri, state);
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException("URL de autorização vazia.");
            }

            return Ok(ToAuthorizeDto(type, url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao gerar URL OAuth para {Marketplace}.", type);

            if (type == MarketplaceType.Shopee)
            {
                var fallback = ShopeeAuthorizationUrlFactory.BuildSandboxFallback(redirectUri, state);
                return Ok(ToAuthorizeDto(type, fallback));
            }

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "Não foi possível gerar a URL de autorização." });
        }
    }

    private static MarketplaceAuthorizeUrlResponseDto ToAuthorizeDto(MarketplaceType type, string url) =>
        new()
        {
            AuthorizeUrl = url,
            AuthorizationUrl = url,
            Marketplace = type.ToString()
        };

    /// <summary>Lista lojas persistidas em MarketplaceAccounts para o usuário autenticado.</summary>
    [HttpGet("lojas")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<IntegracaoLojaResponseDto>> ListLojasAsync(
        [FromQuery] IntegracaoLojaFilter filter,
        CancellationToken cancellationToken)
    {
        var claimValue = ResolveLoggedUserId();
        if (!int.TryParse(claimValue, out var userId))
        {
            return Unauthorized(new IntegracaoLojaResponseDto
            {
                Status = false,
                Descricao = "Usuário não autenticado."
            });
        }

        var result = await _integracaoLojaService.ListByUserAsync(userId, filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>Vinculação manual (homologação). JWT opcional: sem claim usa UserId de fallback no IIS.</summary>
    [HttpPost("vincular-manual")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseDto>> VincularManualAsync(
        [FromBody] IntegracaoLojaDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return InvalidModelState();
            }

            var parsedUserId = GetCurrentUserId();
            var userId = parsedUserId ?? HomologMarketplaceAuth.FallbackUserId;
            if (parsedUserId is null)
            {
                _logger.LogWarning(
                    "vincular-manual sem claim NameIdentifier. Fallback UserId={UserId}.",
                    userId);
            }

            if (dto is null)
            {
                return BadRequest(ResponseDto.Fail("Payload de vinculação inválido."));
            }

            ApplyHomologFallbacks(dto);

            var result = await _integracaoLojaService.LinkAsync(userId, dto, cancellationToken);
            if (!result.Status)
            {
                return BadRequest(ResponseDto.Fail(result.Descricao));
            }

            return Ok(MarketplaceAccountResponseDto.Ok(
                result.Data,
                HomologMarketplaceAuth.ShouldSkipRemoteOAuth(_hostEnvironment.EnvironmentName, dto.AuthorizationCode)
                    ? HomologMarketplaceAuth.ManualLinkSuccessMessage
                    : result.Descricao));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao salvar MarketplaceAccount no SQL Server");
            _logger.LogError(ex, "Erro ao salvar MarketplaceAccount no SQL Server");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ResponseDto.Fail($"Erro do Servidor/SQL: {ex.Message}"));
        }
    }

    /// <summary>Callback OAuth: troca o authorization code por tokens e persiste no banco.</summary>
    [HttpGet("callback")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<MarketplaceTokenResult>> CallbackAsync(
        [FromQuery] MarketplaceType type,
        [FromQuery] string code,
        [FromQuery] string shopId,
        CancellationToken cancellationToken)
    {
        var userId = ResolveLoggedUserId();
        try
        {
            var result = await _marketplaceAuthService.CallbackAndGenerateTokensAsync(
                type, code, shopId, cancellationToken, userId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (DbUpdateException ex)
        {
            Log.Error(ex, "Erro ao salvar MarketplaceAccount no SQL Server");
            _logger.LogError(ex, "Erro ao salvar MarketplaceAccount no SQL Server");
            return BadRequest(MarketplaceTokenFail(shopId, type, MarketplaceAuthService.FormatSqlError(ex)));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao salvar MarketplaceAccount no SQL Server");
            _logger.LogError(ex, "Erro ao salvar MarketplaceAccount no SQL Server");
            return BadRequest(MarketplaceTokenFail(shopId, type, MarketplaceAuthService.FormatSqlError(ex)));
        }
    }

    /// <summary>Retorna access token válido (renova automaticamente se expirado).</summary>
    [HttpGet("token")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<object>> GetValidTokenAsync(
        [FromQuery] string shopId,
        [FromQuery] MarketplaceType type,
        CancellationToken cancellationToken)
    {
        var token = await _marketplaceAuthService.GetValidTokenAsync(shopId, type, cancellationToken);
        return Ok(new { shopId, marketplace = type.ToString(), accessToken = token });
    }

    private static MarketplaceTokenResult MarketplaceTokenFail(
        string shopId,
        MarketplaceType type,
        string message) =>
        new()
        {
            Success = false,
            Descricao = message,
            ShopId = shopId,
            MarketplaceType = type
        };

    private BadRequestObjectResult InvalidModelState()
    {
        var fields = string.Join("; ", ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .Select(entry =>
            {
                var messages = entry.Value!.Errors.Select(error =>
                    string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? error.Exception?.Message ?? "inválido"
                        : error.ErrorMessage);
                return $"{entry.Key}: {string.Join(", ", messages)}";
            }));

        _logger.LogWarning("ModelState inválido ao vincular loja. Campos: {Fields}", fields);
        return BadRequest(ResponseDto.Fail($"Payload de vinculação inválido. {fields}"));
    }

    private void ApplyHomologFallbacks(IntegracaoLojaDto dto)
    {
        if (!_hostEnvironment.IsDevelopment() && !_hostEnvironment.IsEnvironment("Homologacao"))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(dto.FriendlyName))
        {
            dto.FriendlyName = "Loja Homolog";
        }

        if (dto.PlatformType == MarketplaceType.Shopee)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(dto.AuthorizationCode))
        {
            dto.AuthorizationCode = "code_teste";
        }

        if (string.IsNullOrWhiteSpace(dto.ShopId))
        {
            dto.ShopId = "123456";
        }
    }

    private int? GetCurrentUserId()
    {
        var claimValue = ResolveLoggedUserId();
        return int.TryParse(claimValue, out var userId) ? userId : null;
    }

    private string? ResolveLoggedUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? User.FindFirst("sub")?.Value;
}

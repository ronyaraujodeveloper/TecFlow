using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ofertio.API.Security;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Integrations.Shopee;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Enums;
using Ofertio.Core.Security;
using Ofertio.Database.Filter;
using Ofertio.Infrastructure.Services.Integrations.Auth;

namespace Ofertio.API.Controllers;

[ApiController]
[Route("api/marketplace-auth")]
public class MarketplaceAuthController : ControllerBase
{
    private readonly IMarketplaceAuthService _marketplaceAuthService;
    private readonly IIntegracaoLojaService _integracaoLojaService;
    private readonly ILogger<MarketplaceAuthController> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IOAuthStateProtector? _stateProtector;
    private readonly IMemoryCache? _memoryCache;
    private readonly IGlobalIntegrationService? _globalIntegrationService;

    /// <summary>
    /// Não injeta AppDbContext/OfertioDbContext: o EF fica no serviço (DI scoped).
    /// O construtor só recebe abstrações e não dispara I/O nem consulta SQL.
    /// </summary>
    public MarketplaceAuthController(
        IMarketplaceAuthService marketplaceAuthService,
        IIntegracaoLojaService integracaoLojaService,
        ILogger<MarketplaceAuthController> logger,
        IHostEnvironment hostEnvironment,
        IOAuthStateProtector? stateProtector = null,
        IMemoryCache? memoryCache = null,
        IGlobalIntegrationService? globalIntegrationService = null)
    {
        _stateProtector = stateProtector;
        _memoryCache = memoryCache;
        _globalIntegrationService = globalIntegrationService;
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
    public async Task<ActionResult<MarketplaceAuthorizeUrlResponseDto>> GetAuthorizationUrl(
        [FromQuery] MarketplaceType type,
        [FromQuery] string redirectUri,
        [FromQuery] string? state = null,
        [FromQuery] string? friendlyName = null)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return BadRequest(new { error = "redirectUri é obrigatório." });
        }

        var stateValue = await SealOAuthStateAsync(type, state, friendlyName, redirectUri);
        return GenerateAuthorizeResult(type, redirectUri, stateValue);
    }

    /// <summary>Gera URL OAuth por slug da plataforma (shopee / tiktok / mercadolivre).</summary>
    [HttpGet("{plataforma}/authorize-url")]
    [AllowAnonymous]
    public async Task<ActionResult<MarketplaceAuthorizeUrlResponseDto>> GetPlatformAuthorizationUrl(
        string plataforma,
        [FromQuery] string? redirectUri,
        [FromQuery] string? state = null,
        [FromQuery] string? friendlyName = null,
        [FromQuery] string? lojaId = null)
    {
        if (!MarketplacePlatformRoute.TryParse(plataforma, out var type))
        {
            return BadRequest(new { error = "Plataforma inválida. Use shopee, tiktok ou mercadolivre." });
        }

        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return BadRequest(new { error = "redirectUri é obrigatório." });
        }

        var stateValue = string.IsNullOrWhiteSpace(state) ? friendlyName ?? lojaId : state;
        stateValue = await SealOAuthStateAsync(type, stateValue, friendlyName, redirectUri);
        return GenerateAuthorizeResult(type, redirectUri, stateValue);
    }

    /// <summary>
    /// Callback do Mercado Livre sem sessão no navegador: o <c>state</c> selado (AES-GCM) identifica
    /// usuário/tenant, os tokens são gravados em MarketplaceAccounts e a resposta traz a URL de retorno.
    /// </summary>
    [HttpPost("mercadolivre/callback")]
    [AllowAnonymous]
    public async Task<ActionResult<MarketplaceOAuthCallbackResultDto>> CompleteMercadoLivreCallbackAsync(
        [FromBody] MarketplaceOAuthCallbackRequestDto request,
        CancellationToken cancellationToken)
    {
        var outcome = await MercadoLivreOAuthCallbackFlow.CompleteAsync(
            HttpContext,
            request,
            _stateProtector,
            _memoryCache,
            _integracaoLojaService,
            _logger,
            cancellationToken,
            _globalIntegrationService);
        return outcome.StatusCode switch
        {
            StatusCodes.Status200OK => Ok(outcome.Result),
            StatusCodes.Status400BadRequest => BadRequest(outcome.Result),
            _ => StatusCode(outcome.StatusCode, outcome.Result)
        };
    }

    /// <summary>
    /// Callback genérico com <c>state</c> selado: abre o state e despacha para o fluxo da plataforma
    /// (Mercado Livre ou TikTok Shop), Master ou cliente.
    /// </summary>
    [HttpPost("oauth/sealed-callback")]
    [AllowAnonymous]
    public async Task<ActionResult<MarketplaceOAuthCallbackResultDto>> CompleteSealedCallbackAsync(
        [FromBody] MarketplaceOAuthCallbackRequestDto request,
        CancellationToken cancellationToken)
    {
        var platform = _stateProtector is not null && _stateProtector.TryUnprotect(request?.State, out var state) && state is not null
            ? state.Platform
            : MarketplaceType.MercadoLivre;
        var outcome = platform == MarketplaceType.TikTokShop
            ? await TikTokShopOAuthCallbackFlow.CompleteAsync(
                HttpContext,
                request,
                _stateProtector,
                _memoryCache,
                _integracaoLojaService,
                _logger,
                cancellationToken,
                _globalIntegrationService)
            : await MercadoLivreOAuthCallbackFlow.CompleteAsync(
                HttpContext,
                request,
                _stateProtector,
                _memoryCache,
                _integracaoLojaService,
                _logger,
                cancellationToken,
                _globalIntegrationService);
        return outcome.StatusCode switch
        {
            StatusCodes.Status200OK => Ok(outcome.Result),
            StatusCodes.Status400BadRequest => BadRequest(outcome.Result),
            _ => StatusCode(outcome.StatusCode, outcome.Result)
        };
    }

    private async Task<string?> SealOAuthStateAsync(
        MarketplaceType type,
        string? state,
        string? friendlyName,
        string redirectUri)
    {
        if (type is not (MarketplaceType.MercadoLivre or MarketplaceType.TikTokShop) || _stateProtector is null)
        {
            return state;
        }

        var principal = User;
        if (GetCurrentUserId() is null)
        {
            var jwt = await HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            if (jwt.Succeeded && jwt.Principal is not null)
            {
                principal = jwt.Principal;
            }
        }

        var userClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userClaim, out var userId))
        {
            return state;
        }

        Guid? tenantId = Guid.TryParse(principal.FindFirst(OfertioClaimTypes.TenantId)?.Value, out var tenant)
            ? tenant
            : null;
        return _stateProtector.Protect(MarketplaceOAuthStateRules.Create(
            userId,
            tenantId,
            type,
            friendlyName,
            redirectUri));
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
        catch (InvalidOperationException ex) when (type is MarketplaceType.MercadoLivre or MarketplaceType.TikTokShop)
        {
            _logger.LogWarning(ex, "OAuth de {Marketplace} não configurado.", type);
            return BadRequest(new { error = ex.Message });
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

        try
        {
            var result = await _integracaoLojaService.ListByUserAsync(userId, filter, cancellationToken);
            if (!result.Status)
            {
                _logger.LogWarning("Falha ao listar MarketplaceAccounts: {Descricao}", result.Descricao);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao consultar MarketplaceAccounts no SQL Server");
            _logger.LogError(ex, "Erro ao consultar MarketplaceAccounts no SQL Server");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new IntegracaoLojaResponseDto
                {
                    Status = false,
                    Descricao = $"Erro do Servidor/SQL: {ex.Message}"
                });
        }
    }

    /// <summary>Inativa logicamente uma loja (`IsActive = false`) no SQL Server.</summary>
    [HttpDelete("lojas/{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public Task<ActionResult<IntegracaoLojaResponseDto>> UnlinkLojaAsync(
        int id,
        CancellationToken cancellationToken) =>
        UnlinkLojaCoreAsync(id, "DELETE", cancellationToken);

    /// <summary>POST equivalente ao DELETE (IIS/WebDAV costuma bloquear verbos DELETE).</summary>
    [HttpPost("lojas/{id:int}/desconectar")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public Task<ActionResult<IntegracaoLojaResponseDto>> DesconectarLojaAsync(
        int id,
        CancellationToken cancellationToken) =>
        UnlinkLojaCoreAsync(id, "POST", cancellationToken);

    /// <summary>Atualiza apelido e ID de afiliado de uma loja existente (sem criar novo registro).</summary>
    [HttpPut("lojas/{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public Task<ActionResult<IntegracaoLojaResponseDto>> UpdateLojaAsync(
        int id,
        [FromBody] IntegracaoLojaDto dto,
        CancellationToken cancellationToken) =>
        UpdateLojaCoreAsync(id, dto, "PUT", cancellationToken);

    /// <summary>POST equivalente ao PUT (IIS/WebDAV costuma bloquear verbos PUT).</summary>
    [HttpPost("lojas/{id:int}/editar")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public Task<ActionResult<IntegracaoLojaResponseDto>> EditarLojaAsync(
        int id,
        [FromBody] IntegracaoLojaDto dto,
        CancellationToken cancellationToken) =>
        UpdateLojaCoreAsync(id, dto, "POST", cancellationToken);

    private async Task<ActionResult<IntegracaoLojaResponseDto>> UpdateLojaCoreAsync(
        int id,
        IntegracaoLojaDto dto,
        string httpMethod,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return InvalidModelState();
        }

        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new IntegracaoLojaResponseDto
            {
                Status = false,
                Descricao = "Usuário não autenticado."
            });
        }

        try
        {
            Console.WriteLine($"[MarketplaceAuth] {httpMethod} lojas/{id} editar userId={userId.Value}");
            var result = await _integracaoLojaService.UpdateAsync(userId.Value, id, dto, cancellationToken);
            return result.Status ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao atualizar MarketplaceAccount {AccountId} no SQL Server", id);
            _logger.LogError(ex, "Erro ao atualizar MarketplaceAccount {AccountId} no SQL Server", id);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new IntegracaoLojaResponseDto
                {
                    Status = false,
                    Descricao = $"Erro do Servidor/SQL: {ex.Message}"
                });
        }
    }

    private async Task<ActionResult<IntegracaoLojaResponseDto>> UnlinkLojaCoreAsync(
        int id,
        string httpMethod,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new IntegracaoLojaResponseDto
            {
                Status = false,
                Descricao = "Usuário não autenticado."
            });
        }

        try
        {
            Console.WriteLine($"[MarketplaceAuth] {httpMethod} lojas/{id} desconectar userId={userId.Value}");
            var result = await _integracaoLojaService.UnlinkAsync(userId.Value, id, cancellationToken);
            return result.Status ? Ok(result) : NotFound(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO] {httpMethod} lojas/{id}: {ex.Message} - {ex.StackTrace}");
            Log.Error(ex, "Erro ao inativar MarketplaceAccount {AccountId} no SQL Server", id);
            _logger.LogError(ex, "Erro ao inativar MarketplaceAccount {AccountId} no SQL Server", id);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new IntegracaoLojaResponseDto
                {
                    Status = false,
                    Descricao = $"Erro do Servidor/SQL: {ex.Message}"
                });
        }
    }

    /// <summary>Expande encurtadores (br.shp.ee, amzn.to, magalu.me) e extrai o Tracking ID.</summary>
    [HttpPost("expand-affiliate-url")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<ExpandAffiliateUrlResponseDto>> ExpandAffiliateUrlAsync(
        [FromBody] ExpandAffiliateUrlRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _integracaoLojaService.ExpandAffiliateUrlAsync(
            request?.Url ?? string.Empty,
            request?.Platform ?? string.Empty,
            cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
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

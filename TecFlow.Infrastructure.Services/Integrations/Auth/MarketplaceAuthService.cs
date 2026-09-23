using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;

namespace TecFlow.Infrastructure.Services.Integrations.Auth;

public class MarketplaceAuthService : IMarketplaceAuthService
{
    private static readonly TimeSpan ExpirySkew = TimeSpan.FromMinutes(5);

    private readonly IMarketplaceTokenRepository _tokenRepository;
    private readonly IMarketplaceAccountRepository _accountRepository;
    private readonly AppDbContext _context;
    private readonly ICurrentTenantService _currentTenant;
    private readonly IMarketplaceSignatureService _signatureService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TikTokShopIntegrationOptions _tikTokOptions;
    private readonly ShopeeIntegrationOptions _shopeeOptions;
    private readonly ILogger<MarketplaceAuthService> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ITenantProvisioningService _tenantProvisioning;

    public MarketplaceAuthService(
        IMarketplaceTokenRepository tokenRepository,
        IMarketplaceAccountRepository accountRepository,
        AppDbContext context,
        ICurrentTenantService currentTenant,
        IMarketplaceSignatureService signatureService,
        IHttpClientFactory httpClientFactory,
        IOptions<TikTokShopIntegrationOptions> tikTokOptions,
        IOptions<ShopeeIntegrationOptions> shopeeOptions,
        ILogger<MarketplaceAuthService> logger,
        IHostEnvironment hostEnvironment,
        ITenantProvisioningService tenantProvisioning)
    {
        _tokenRepository = tokenRepository;
        _accountRepository = accountRepository;
        _context = context;
        _currentTenant = currentTenant;
        _signatureService = signatureService;
        _httpClientFactory = httpClientFactory;
        _tikTokOptions = tikTokOptions.Value;
        _shopeeOptions = shopeeOptions.Value;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _tenantProvisioning = tenantProvisioning;
    }

    public string GenerateAuthorizationUrl(MarketplaceType type, string redirectUri, string? state = null)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            throw new ArgumentException("redirectUri é obrigatório.", nameof(redirectUri));
        }

        var stateValue = string.IsNullOrWhiteSpace(state)
            ? Guid.NewGuid().ToString("N")
            : state;

        return type switch
        {
            MarketplaceType.TikTokShop => BuildTikTokAuthorizationUrl(redirectUri, stateValue),
            MarketplaceType.Shopee => BuildShopeeAuthorizationUrl(redirectUri, stateValue),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Marketplace não suportado.")
        };
    }

    public async Task<MarketplaceTokenResult> CallbackAndGenerateTokensAsync(
        MarketplaceType type,
        string code,
        string shopId,
        CancellationToken cancellationToken = default,
        string? userId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Fail(type, shopId, "Código de autorização inválido.");
        }

        if (string.IsNullOrWhiteSpace(shopId))
        {
            return Fail(type, shopId, "ShopId é obrigatório.");
        }

        try
        {
            OAuthTokenPayload tokenPayload;
            if (ShouldSkipRemoteOAuth(code))
            {
                _logger.LogWarning(
                    "Pulando OAuth remoto da Shopee/TikTok para {Marketplace} shop {ShopId} (homologação/code_*).",
                    type,
                    shopId);
                tokenPayload = new OAuthTokenPayload
                {
                    AccessToken = HomologMarketplaceAuth.StubAccessToken,
                    RefreshToken = HomologMarketplaceAuth.StubRefreshToken,
                    AccessTokenLifetimeSeconds = HomologMarketplaceAuth.StubAccessTokenLifetimeSeconds,
                    RefreshTokenLifetimeSeconds = HomologMarketplaceAuth.StubAccessTokenLifetimeSeconds
                };
            }
            else
            {
                tokenPayload = type switch
                {
                    MarketplaceType.TikTokShop => await ExchangeTikTokCodeAsync(code, cancellationToken),
                    MarketplaceType.Shopee => await ExchangeShopeeCodeAsync(code, shopId, cancellationToken),
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };
            }

            if (string.IsNullOrWhiteSpace(tokenPayload.AccessToken))
            {
                return Fail(type, shopId, "Access token não retornado pela plataforma.");
            }

            var tenant = await _tenantProvisioning.EnsurePersistedTenantAsync(
                _currentTenant.TenantId,
                cancellationToken);
            var tenantId = tenant.Id;

            var entity = new MarketplaceToken
            {
                TenantId = tenantId,
                ShopId = shopId,
                MarketplaceType = type,
                AccessToken = tokenPayload.AccessToken,
                RefreshToken = tokenPayload.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenPayload.AccessTokenLifetimeSeconds),
                RefreshExpiresAt = tokenPayload.RefreshTokenLifetimeSeconds > 0
                    ? DateTime.UtcNow.AddSeconds(tokenPayload.RefreshTokenLifetimeSeconds)
                    : null
            };

            entity.Touch();
            await _tokenRepository.UpsertAsync(entity);

            await PersistMarketplaceAccountAsync(new MarketplaceAccount
            {
                TenantId = tenantId,
                UserId = string.IsNullOrWhiteSpace(userId) ? string.Empty : userId.Trim(),
                ShopId = shopId.Trim(),
                ShopName = shopId.Trim(),
                FriendlyName = shopId.Trim(),
                MarketplaceType = type,
                AccessToken = entity.AccessToken,
                RefreshToken = entity.RefreshToken,
                IsActive = true,
                ExpiresAt = entity.ExpiresAt,
                RefreshExpiresAt = entity.RefreshExpiresAt,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return new MarketplaceTokenResult
            {
                Success = true,
                Descricao = ShouldSkipRemoteOAuth(code)
                    ? HomologMarketplaceAuth.ManualLinkSuccessMessage
                    : "Tokens armazenados com sucesso.",
                ShopId = shopId,
                MarketplaceType = type,
                ExpiresAt = entity.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha no callback OAuth para {Marketplace} shop {ShopId}.", type, shopId);
            return Fail(type, shopId, FormatSqlError(ex));
        }
    }

    public async Task<string> GetValidTokenAsync(
        string shopId,
        MarketplaceType type,
        CancellationToken cancellationToken = default)
    {
        var stored = await _tokenRepository.GetByShopAndMarketplaceAsync(shopId, type)
            ?? throw new InvalidOperationException(
                $"Nenhum token encontrado para shop '{shopId}' e marketplace '{type}'.");

        if (stored.ExpiresAt > DateTime.UtcNow.Add(ExpirySkew))
        {
            return stored.AccessToken;
        }

        if (string.IsNullOrWhiteSpace(stored.RefreshToken))
        {
            throw new InvalidOperationException(
                $"Token expirado e sem refresh token para shop '{shopId}' ({type}). Reautorize a loja.");
        }

        var refreshed = type switch
        {
            MarketplaceType.TikTokShop => await RefreshTikTokTokenAsync(stored.RefreshToken, cancellationToken),
            MarketplaceType.Shopee => await RefreshShopeeTokenAsync(stored, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        stored.AccessToken = refreshed.AccessToken;
        stored.RefreshToken = refreshed.RefreshToken ?? stored.RefreshToken;
        stored.ExpiresAt = DateTime.UtcNow.AddSeconds(refreshed.AccessTokenLifetimeSeconds);
        if (refreshed.RefreshTokenLifetimeSeconds > 0)
        {
            stored.RefreshExpiresAt = DateTime.UtcNow.AddSeconds(refreshed.RefreshTokenLifetimeSeconds);
        }

        stored.Touch();
        await _tokenRepository.UpsertAsync(stored);

        var account = await _context.MarketplaceAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                item => item.ShopId == shopId && item.MarketplaceType == type,
                cancellationToken);
        if (account is not null)
        {
            account.AccessToken = stored.AccessToken;
            account.RefreshToken = stored.RefreshToken;
            account.ExpiresAt = stored.ExpiresAt;
            account.RefreshExpiresAt = stored.RefreshExpiresAt;
            account.IsActive = true;
            account.Touch();
            await _context.SaveChangesAsync(cancellationToken);
        }

        return stored.AccessToken;
    }

    private string BuildTikTokAuthorizationUrl(string redirectUri, string state)
    {
        EnsureTikTokCredentials();

        var query = new Dictionary<string, string?>
        {
            ["app_key"] = _tikTokOptions.AppKey,
            ["state"] = state,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorized_code"
        };

        return AppendQuery(_tikTokOptions.AuthorizeUrl, query);
    }

    private string BuildShopeeAuthorizationUrl(string redirectUri, string state)
    {
        try
        {
            var usingSandbox = !ShopeeAuthorizationUrlFactory.HasLiveCredentials(
                _shopeeOptions.PartnerId,
                _shopeeOptions.PartnerKey);

            var partnerId = ShopeeAuthorizationUrlFactory.ResolvePartnerId(_shopeeOptions.PartnerId);
            var partnerKey = ShopeeAuthorizationUrlFactory.ResolvePartnerKey(_shopeeOptions.PartnerKey);

            if (usingSandbox)
            {
                _logger.LogWarning(
                    "Integrations:Shopee PartnerId/PartnerKey vazios. Gerando URL sandbox {AuthUrl}.",
                    ShopeeAuthorizationUrlFactory.AuthPartnerAbsoluteUrl);
            }

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var apiPath = NormalizeShopeePath(
                string.IsNullOrWhiteSpace(_shopeeOptions.AuthPartnerPath)
                    ? "shop/auth_partner"
                    : _shopeeOptions.AuthPartnerPath);
            var sign = _signatureService.GenerateShopeeSign(
                partnerId,
                partnerKey,
                apiPath,
                timestamp);

            var redirectWithState = AppendQuery(redirectUri, new Dictionary<string, string?>
            {
                ["state"] = state
            });

            var query = new Dictionary<string, string?>
            {
                ["partner_id"] = partnerId,
                ["timestamp"] = timestamp.ToString(),
                ["sign"] = sign,
                ["redirect"] = redirectWithState
            };

            var authUrl = ShopeeAuthorizationUrlFactory.ResolveAuthPartnerUrl(
                _shopeeOptions.ApiBaseUrl,
                _shopeeOptions.AuthPartnerPath);

            return AppendQuery(authUrl, query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao assinar URL OAuth Shopee. Usando fallback sandbox.");
            return ShopeeAuthorizationUrlFactory.BuildSandboxFallback(redirectUri, state);
        }
    }

    private async Task<OAuthTokenPayload> ExchangeTikTokCodeAsync(string code, CancellationToken cancellationToken)
    {
        EnsureTikTokCredentials();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var apiPath = "/api/v2/token/get";
        var bodyObject = new
        {
            app_key = _tikTokOptions.AppKey,
            app_secret = _tikTokOptions.AppSecret,
            auth_code = code,
            grant_type = "authorized_code"
        };

        var bodyJson = JsonSerializer.Serialize(bodyObject);
        var sign = _signatureService.GenerateTikTokShopSign(
            _tikTokOptions.AppKey,
            _tikTokOptions.AppSecret,
            apiPath,
            timestamp,
            bodyJson);

        var url = AppendQuery(_tikTokOptions.TokenUrl, new Dictionary<string, string?>
        {
            ["app_key"] = _tikTokOptions.AppKey,
            ["timestamp"] = timestamp.ToString(),
            ["sign"] = sign
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
        };

        return await SendTokenRequestAsync(request, cancellationToken);
    }

    private async Task<OAuthTokenPayload> ExchangeShopeeCodeAsync(
        string code,
        string shopId,
        CancellationToken cancellationToken)
    {
        EnsureShopeeCredentials();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var apiPath = NormalizeShopeePath(_shopeeOptions.TokenPath);
        var sign = _signatureService.GenerateShopeeSign(
            _shopeeOptions.PartnerId,
            _shopeeOptions.PartnerKey,
            apiPath,
            timestamp);

        var url = AppendQuery(
            $"{_shopeeOptions.ApiBaseUrl.TrimEnd('/')}/{_shopeeOptions.TokenPath.TrimStart('/')}",
            new Dictionary<string, string?>
            {
                ["partner_id"] = _shopeeOptions.PartnerId,
                ["timestamp"] = timestamp.ToString(),
                ["sign"] = sign
            });

        object shopIdValue = long.TryParse(shopId, out var parsedShopId) ? parsedShopId : shopId;
        object partnerIdValue = int.TryParse(_shopeeOptions.PartnerId, out var partnerId)
            ? partnerId
            : _shopeeOptions.PartnerId;

        var body = new { code, shop_id = shopIdValue, partner_id = partnerIdValue };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };

        return await SendTokenRequestAsync(request, cancellationToken);
    }

    private async Task<OAuthTokenPayload> RefreshTikTokTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        EnsureTikTokCredentials();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var apiPath = "/api/v2/token/refresh";
        var bodyObject = new
        {
            app_key = _tikTokOptions.AppKey,
            app_secret = _tikTokOptions.AppSecret,
            refresh_token = refreshToken,
            grant_type = "refresh_token"
        };

        var bodyJson = JsonSerializer.Serialize(bodyObject);
        var sign = _signatureService.GenerateTikTokShopSign(
            _tikTokOptions.AppKey,
            _tikTokOptions.AppSecret,
            apiPath,
            timestamp,
            bodyJson);

        var url = AppendQuery(_tikTokOptions.RefreshTokenUrl, new Dictionary<string, string?>
        {
            ["app_key"] = _tikTokOptions.AppKey,
            ["timestamp"] = timestamp.ToString(),
            ["sign"] = sign
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
        };

        return await SendTokenRequestAsync(request, cancellationToken);
    }

    private async Task<OAuthTokenPayload> RefreshShopeeTokenAsync(
        MarketplaceToken stored,
        CancellationToken cancellationToken)
    {
        EnsureShopeeCredentials();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var apiPath = NormalizeShopeePath(_shopeeOptions.RefreshTokenPath);
        var sign = _signatureService.GenerateShopeeSign(
            _shopeeOptions.PartnerId,
            _shopeeOptions.PartnerKey,
            apiPath,
            timestamp,
            stored.AccessToken,
            stored.ShopId);

        var url = AppendQuery(
            $"{_shopeeOptions.ApiBaseUrl.TrimEnd('/')}/{_shopeeOptions.RefreshTokenPath.TrimStart('/')}",
            new Dictionary<string, string?>
            {
                ["partner_id"] = _shopeeOptions.PartnerId,
                ["timestamp"] = timestamp.ToString(),
                ["sign"] = sign,
                ["shop_id"] = stored.ShopId,
                ["access_token"] = stored.AccessToken
            });

        object partnerIdValue = int.TryParse(_shopeeOptions.PartnerId, out var partnerId)
            ? partnerId
            : _shopeeOptions.PartnerId;
        object shopIdValue = long.TryParse(stored.ShopId, out var parsedShopId)
            ? parsedShopId
            : stored.ShopId;

        var body = new
        {
            refresh_token = stored.RefreshToken,
            partner_id = partnerIdValue,
            shop_id = shopIdValue
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };

        return await SendTokenRequestAsync(request, cancellationToken);
    }

    private async Task<OAuthTokenPayload> SendTokenRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("TecFlow.MarketplaceOAuth");
        using var response = await client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OAuth token request failed: {Status} — {Body}", (int)response.StatusCode, content);
            throw new InvalidOperationException($"Falha na API OAuth ({(int)response.StatusCode}).");
        }

        var parsed = JsonSerializer.Deserialize<MarketplaceOAuthTokenResponse>(content)
            ?? throw new InvalidOperationException("Resposta OAuth inválida.");

        return new OAuthTokenPayload
        {
            AccessToken = parsed.ResolveAccessToken() ?? string.Empty,
            RefreshToken = parsed.ResolveRefreshToken(),
            AccessTokenLifetimeSeconds = parsed.ResolveAccessTokenLifetimeSeconds(),
            RefreshTokenLifetimeSeconds = parsed.RefreshTokenExpireIn ?? parsed.Data?.RefreshTokenExpireIn ?? 0
        };
    }

    private static string NormalizeShopeePath(string relativePath)
    {
        var path = relativePath.Trim();
        return path.StartsWith("api/v2/", StringComparison.OrdinalIgnoreCase)
            ? "/" + path
            : "/api/v2/" + path.TrimStart('/');
    }

    private static string AppendQuery(string baseUrl, Dictionary<string, string?> query)
    {
        var separator = baseUrl.Contains('?') ? '&' : '?';
        var pairs = query
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");

        return $"{baseUrl}{separator}{string.Join('&', pairs)}";
    }

    private void EnsureTikTokCredentials()
    {
        if (string.IsNullOrWhiteSpace(_tikTokOptions.AppKey) || string.IsNullOrWhiteSpace(_tikTokOptions.AppSecret))
        {
            throw new InvalidOperationException(
                $"Configure {TikTokShopIntegrationOptions.SectionName}:AppKey e AppSecret.");
        }
    }

    private void EnsureShopeeCredentials()
    {
        if (string.IsNullOrWhiteSpace(_shopeeOptions.PartnerId) || string.IsNullOrWhiteSpace(_shopeeOptions.PartnerKey))
        {
            throw new InvalidOperationException(
                $"Configure {ShopeeIntegrationOptions.SectionName}:PartnerId e PartnerKey.");
        }
    }

    private bool ShouldSkipRemoteOAuth(string code) =>
        HomologMarketplaceAuth.ShouldSkipRemoteOAuth(_hostEnvironment.EnvironmentName, code);

    private async Task PersistMarketplaceAccountAsync(MarketplaceAccount account, CancellationToken cancellationToken)
    {
        var tenant = await _tenantProvisioning.EnsurePersistedTenantAsync(
            account.TenantId == Guid.Empty ? (Guid?)null : account.TenantId,
            cancellationToken);
        account.TenantId = tenant.Id;

        EnsureMarketplaceAccountCanBeInserted(account);

        try
        {
            var existing = await _context.MarketplaceAccounts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    item => item.TenantId == account.TenantId
                        && item.ShopId == account.ShopId
                        && item.MarketplaceType == account.MarketplaceType,
                    cancellationToken);

            if (existing is null)
            {
                _context.MarketplaceAccounts.Add(account);
            }
            else
            {
                existing.UserId = string.IsNullOrWhiteSpace(account.UserId) ? existing.UserId : account.UserId;
                existing.FriendlyName = account.FriendlyName;
                existing.ShopName = account.ShopName;
                existing.AccessToken = account.AccessToken;
                existing.RefreshToken = account.RefreshToken;
                existing.ExpiresAt = account.ExpiresAt;
                existing.RefreshExpiresAt = account.RefreshExpiresAt;
                existing.IsActive = account.IsActive;
                existing.MarketplaceType = account.MarketplaceType;
                if (!string.IsNullOrWhiteSpace(account.AffiliateTrackingId))
                {
                    existing.AffiliateTrackingId = account.AffiliateTrackingId;
                    existing.TrackingId = account.AffiliateTrackingId;
                }
                if (!string.IsNullOrWhiteSpace(account.TrackingId))
                {
                    existing.TrackingId = account.TrackingId;
                    existing.AffiliateTrackingId = string.IsNullOrWhiteSpace(existing.AffiliateTrackingId)
                        ? account.TrackingId
                        : existing.AffiliateTrackingId;
                }
                if (!string.IsNullOrWhiteSpace(account.AppKey))
                {
                    existing.AppKey = account.AppKey;
                }
                if (!string.IsNullOrWhiteSpace(account.AppSecret))
                {
                    existing.AppSecret = account.AppSecret;
                }
                existing.Touch();
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            Log.Error(ex, "Erro ao salvar MarketplaceAccount no SQL Server");
            _logger.LogError(ex, "Erro ao salvar MarketplaceAccount no SQL Server");
            throw;
        }
        catch (SqlException ex)
        {
            Log.Error(ex, "Erro ao salvar MarketplaceAccount no SQL Server");
            _logger.LogError(ex, "Erro ao salvar MarketplaceAccount no SQL Server");
            throw;
        }
    }

    private static void EnsureMarketplaceAccountCanBeInserted(MarketplaceAccount account)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(account.UserId))
        {
            missing.Add(nameof(account.UserId));
        }

        if (string.IsNullOrWhiteSpace(account.ShopId))
        {
            missing.Add(nameof(account.ShopId));
        }

        if (string.IsNullOrWhiteSpace(account.FriendlyName))
        {
            missing.Add(nameof(account.FriendlyName));
        }

        if (!account.MarketplaceType.IsUniversalAffiliatePlatform())
        {
            missing.Add("PlatformType");
        }

        if (string.IsNullOrWhiteSpace(account.AccessToken))
        {
            missing.Add(nameof(account.AccessToken));
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "MarketplaceAccount incompleta para persistência no SQL Server: " + string.Join(", ", missing) + ".");
        }
    }

    public static string FormatSqlError(Exception ex)
    {
        var parts = new List<string>();
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(current.Message) && !parts.Contains(current.Message))
            {
                parts.Add(current.Message);
            }
        }

        return parts.Count == 0 ? "Erro ao salvar MarketplaceAccount no SQL Server." : string.Join(" | ", parts);
    }

    private static MarketplaceTokenResult Fail(MarketplaceType type, string shopId, string message) =>
        new()
        {
            Success = false,
            Descricao = message,
            ShopId = shopId,
            MarketplaceType = type
        };

    private sealed class OAuthTokenPayload
    {
        public string AccessToken { get; init; } = string.Empty;
        public string? RefreshToken { get; init; }
        public int AccessTokenLifetimeSeconds { get; init; }
        public int RefreshTokenLifetimeSeconds { get; init; }
    }
}

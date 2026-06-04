using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database.MultiTenancy;

namespace TecFlow.Infrastructure.Services.Integrations.Auth;

public class MarketplaceAuthService : IMarketplaceAuthService
{
    private static readonly TimeSpan ExpirySkew = TimeSpan.FromMinutes(5);

    private readonly IMarketplaceTokenRepository _tokenRepository;
    private readonly IMarketplaceAccountRepository _accountRepository;
    private readonly ICurrentTenantService _currentTenant;
    private readonly IMarketplaceSignatureService _signatureService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TikTokShopIntegrationOptions _tikTokOptions;
    private readonly ShopeeIntegrationOptions _shopeeOptions;
    private readonly ILogger<MarketplaceAuthService> _logger;

    public MarketplaceAuthService(
        IMarketplaceTokenRepository tokenRepository,
        IMarketplaceAccountRepository accountRepository,
        ICurrentTenantService currentTenant,
        IMarketplaceSignatureService signatureService,
        IHttpClientFactory httpClientFactory,
        IOptions<TikTokShopIntegrationOptions> tikTokOptions,
        IOptions<ShopeeIntegrationOptions> shopeeOptions,
        ILogger<MarketplaceAuthService> logger)
    {
        _tokenRepository = tokenRepository;
        _accountRepository = accountRepository;
        _currentTenant = currentTenant;
        _signatureService = signatureService;
        _httpClientFactory = httpClientFactory;
        _tikTokOptions = tikTokOptions.Value;
        _shopeeOptions = shopeeOptions.Value;
        _logger = logger;
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
            MarketplaceType.Shopee => BuildShopeeAuthorizationUrl(redirectUri),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Marketplace não suportado.")
        };
    }

    public async Task<MarketplaceTokenResult> CallbackAndGenerateTokensAsync(
        MarketplaceType type,
        string code,
        string shopId,
        CancellationToken cancellationToken = default)
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
            var tokenPayload = type switch
            {
                MarketplaceType.TikTokShop => await ExchangeTikTokCodeAsync(code, cancellationToken),
                MarketplaceType.Shopee => await ExchangeShopeeCodeAsync(code, shopId, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            if (string.IsNullOrWhiteSpace(tokenPayload.AccessToken))
            {
                return Fail(type, shopId, "Access token não retornado pela plataforma.");
            }

            var tenantId = await ResolveTenantIdForShopAsync(shopId, type);
            if (tenantId is null)
            {
                return Fail(type, shopId, "Faça login no painel TecFlow antes de vincular uma nova loja.");
            }

            var entity = new MarketplaceToken
            {
                TenantId = tenantId.Value,
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

            await _accountRepository.UpsertAsync(new MarketplaceAccount
            {
                TenantId = tenantId.Value,
                ShopId = shopId,
                ShopName = shopId,
                MarketplaceType = type,
                AccessToken = entity.AccessToken,
                RefreshToken = entity.RefreshToken,
                ExpiresAt = entity.ExpiresAt,
                RefreshExpiresAt = entity.RefreshExpiresAt
            });

            return new MarketplaceTokenResult
            {
                Success = true,
                Descricao = "Tokens armazenados com sucesso.",
                ShopId = shopId,
                MarketplaceType = type,
                ExpiresAt = entity.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha no callback OAuth para {Marketplace} shop {ShopId}.", type, shopId);
            return Fail(type, shopId, ex.Message);
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

    private string BuildShopeeAuthorizationUrl(string redirectUri)
    {
        EnsureShopeeCredentials();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var apiPath = NormalizeShopeePath(_shopeeOptions.AuthPartnerPath);
        var sign = _signatureService.GenerateShopeeSign(
            _shopeeOptions.PartnerId,
            _shopeeOptions.PartnerKey,
            apiPath,
            timestamp);

        var query = new Dictionary<string, string?>
        {
            ["partner_id"] = _shopeeOptions.PartnerId,
            ["timestamp"] = timestamp.ToString(),
            ["sign"] = sign,
            ["redirect"] = redirectUri
        };

        return AppendQuery($"{_shopeeOptions.ApiBaseUrl.TrimEnd('/')}/{_shopeeOptions.AuthPartnerPath.TrimStart('/')}", query);
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

    private async Task<Guid?> ResolveTenantIdForShopAsync(string shopId, MarketplaceType type)
    {
        if (_currentTenant.TenantId is { } activeTenant && activeTenant != Guid.Empty)
        {
            return activeTenant;
        }

        var existing = await _tokenRepository.GetByShopAndMarketplaceIgnoreTenantAsync(shopId, type);
        if (existing is null || existing.TenantId == Guid.Empty)
        {
            return null;
        }

        return existing.TenantId;
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

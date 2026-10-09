using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.MercadoLivre;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class MercadoLivreApiService : IMercadoLivreApiService
{
    public const string PublicUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private static readonly JsonSerializerOptions SearchJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly MercadoLivreIntegrationOptions _options;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<MercadoLivreApiService> _logger;

    public MercadoLivreApiService(
        HttpClient httpClient,
        IOptions<MercadoLivreIntegrationOptions> options,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ILogger<MercadoLivreApiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        EnsurePublicUserAgent(_httpClient);
    }

    public async Task<OfficialOfferSnapshotDto?> GetItemAsync(string itemId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.mercadolibre.com/items/{Uri.EscapeDataString(itemId.Trim())}");
            ApplyBrowserHeaders(request);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro API ML [{StatusCode}]: {Body}", response.StatusCode, json);
                return null;
            }

            return MercadoLivreItemParser.Parse(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao consultar item Mercado Livre '{Item}'", itemId);
            return null;
        }
    }

    public async Task<OfficialCatalogChannelResult> SearchProductsAsync(
        int userId,
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return OfficialCatalogChannelResult.Empty;
        }

        query = query.Trim();
        _ = limit;
        try
        {
            var account = await LoadMercadoLivreAccountAsync(userId, cancellationToken);
            var affiliateId = OfficialCatalogSearchRules.ResolveMercadoLivreAffiliateId(
                account?.TrackingId,
                account?.AffiliateTrackingId);
            var connectedByAffiliate = !string.IsNullOrWhiteSpace(affiliateId);
            var accessToken = await ResolveAccessTokenAsync(account, cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken) && !connectedByAffiliate)
            {
                return OfficialCatalogChannelResult.Unconfigured();
            }

            var searchUri =
                $"https://api.mercadolibre.com/sites/MLB/search?q={Uri.EscapeDataString(query)}&limit=20";
            var (statusCode, body) = await SendSearchAsync(searchUri, accessToken, cancellationToken);
            if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                && !string.IsNullOrWhiteSpace(accessToken))
            {
                (statusCode, body) = await SendSearchAsync(searchUri, accessToken: null, cancellationToken);
            }

            if (statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Erro API ML [{StatusCode}]: {Body}", statusCode, body);
                if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                    && !connectedByAffiliate)
                {
                    return OfficialCatalogChannelResult.Unconfigured();
                }

                return OfficialCatalogChannelResult.Failed(
                    $"Erro HTTP {(int)statusCode} ({statusCode})");
            }

            MercadoLivreSearchResponse? payload;
            try
            {
                payload = JsonSerializer.Deserialize<MercadoLivreSearchResponse>(body, SearchJsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Exceção ao buscar no Mercado Livre para query '{Query}'", query);
                throw;
            }

            if (payload?.Results == null || payload.Results.Count == 0)
            {
                return OfficialCatalogChannelResult.Empty;
            }

            var mattWord = string.IsNullOrWhiteSpace(account?.FriendlyName)
                ? affiliateId ?? string.Empty
                : account.FriendlyName.Trim();
            var items = payload.Results
                .Select(item => new OfficialCatalogProductDto
                {
                    Platform = "Mercado Livre",
                    PlatformType = MarketplaceType.MercadoLivre,
                    ProductId = string.IsNullOrWhiteSpace(item.Id) ? null : item.Id,
                    ProductName = string.IsNullOrWhiteSpace(item.Title) ? null : item.Title,
                    Price = item.Price,
                    OriginalPrice = item.OriginalPrice,
                    ImageUrl = string.IsNullOrWhiteSpace(item.SecureThumbnail)
                        ? item.Thumbnail?.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase)
                        : item.SecureThumbnail.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase),
                    SourceUrl = ApplyAffiliateToPermalink(item.Permalink, affiliateId, mattWord),
                    Shipping = item.Shipping?.FreeShipping == true ? "Frete grátis" : null,
                    Source = "Api"
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.SourceUrl))
                .ToList();

            return OfficialCatalogChannelResult.From(items);
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao buscar no Mercado Livre para query '{Query}'", query);
            throw;
        }
    }

    internal static void EnsurePublicUserAgent(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        httpClient.DefaultRequestHeaders.Remove("User-Agent");
        ApplyHeader(httpClient.DefaultRequestHeaders, "User-Agent", PublicUserAgent);
        if (!httpClient.DefaultRequestHeaders.Accept.Any(item => item.MediaType == "application/json"))
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    private async Task<(HttpStatusCode StatusCode, string Body)> SendSearchAsync(
        string searchUri,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, searchUri);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        ApplyBrowserHeaders(request);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return (response.StatusCode, body);
    }

    private async Task<string?> ResolveAccessTokenAsync(
        MarketplaceAccount? account,
        CancellationToken cancellationToken)
    {
        if (account is not null && IsTokenFresh(account))
        {
            return account.AccessToken!.Trim();
        }

        if (account is not null
            && !string.IsNullOrWhiteSpace(account.RefreshToken)
            && TryResolveAppCredentials(account, out var refreshAppId, out var refreshSecret))
        {
            var refreshed = await RequestOAuthTokenAsync(
                "refresh_token",
                refreshAppId,
                refreshSecret,
                account.RefreshToken,
                cancellationToken);
            if (!string.IsNullOrWhiteSpace(refreshed))
            {
                return refreshed;
            }
        }

        if (_options.HasAccessToken)
        {
            return _options.AccessToken.Trim();
        }

        if (_options.HasAppCredentials)
        {
            return await RequestOAuthTokenAsync(
                "client_credentials",
                _options.AppId.Trim(),
                _options.SecretKey.Trim(),
                refreshToken: null,
                cancellationToken);
        }

        return null;
    }

    private async Task<MarketplaceAccount?> LoadMercadoLivreAccountAsync(int userId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var userKey = userId.ToString(CultureInfo.InvariantCulture);
        var accounts = await dbContext.MarketplaceAccounts
            .AsNoTracking()
            .Where(x => x.IsActive
                && (x.MarketplaceType == MarketplaceType.MercadoLivre
                    || (x.FriendlyName != null && x.FriendlyName.Replace(" ", "") == "MercadoLivre")
                    || (x.ShopName != null && x.ShopName.Replace(" ", "") == "MercadoLivre")))
            .ToListAsync(cancellationToken);

        return accounts
            .Where(item => OfficialCatalogSearchRules.MatchesMercadoLivreAccount(
                item.MarketplaceType,
                item.FriendlyName,
                item.ShopName))
            .OrderByDescending(item => OwnsAccount(item, userKey))
            .ThenByDescending(item =>
                !string.IsNullOrWhiteSpace(
                    OfficialCatalogSearchRules.ResolveMercadoLivreAffiliateId(
                        item.TrackingId,
                        item.AffiliateTrackingId)))
            .ThenByDescending(item => item.ExpiresAt)
            .FirstOrDefault();
    }

    private static bool OwnsAccount(MarketplaceAccount account, string userKey) =>
        string.IsNullOrWhiteSpace(account.UserId)
        || string.Equals(account.UserId.Trim(), userKey, StringComparison.Ordinal);

    private static string ApplyAffiliateToPermalink(string? permalink, string? affiliateId, string mattWord)
    {
        if (string.IsNullOrWhiteSpace(permalink))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(affiliateId))
        {
            return permalink;
        }

        try
        {
            return MercadoLivreCommissionUrlBuilder.ApplyMattParams(permalink, affiliateId, mattWord);
        }
        catch (Exception)
        {
            return permalink;
        }
    }

    private async Task<string?> RequestOAuthTokenAsync(
        string grantType,
        string appId,
        string secret,
        string? refreshToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var fields = new Dictionary<string, string>
            {
                ["grant_type"] = grantType,
                ["client_id"] = appId,
                ["client_secret"] = secret
            };
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                fields["refresh_token"] = refreshToken;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl)
            {
                Content = new FormUrlEncodedContent(fields)
            };
            ApplyBrowserHeaders(request);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Falha ao renovar token ML. Status={Status} Body={Body}", response.StatusCode, json);
                return null;
            }

            var payload = JsonSerializer.Deserialize<MercadoLivreTokenResponse>(json, SearchJsonOptions);
            return string.IsNullOrWhiteSpace(payload?.AccessToken) ? null : payload.AccessToken.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exceção ao renovar token do Mercado Livre.");
            return null;
        }
    }

    private bool TryResolveAppCredentials(MarketplaceAccount account, out string appId, out string secret)
    {
        appId = FirstNonEmpty(account.AppKey, _options.AppId) ?? string.Empty;
        secret = FirstNonEmpty(account.AppSecret, _options.SecretKey) ?? string.Empty;
        return !string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(secret);
    }

    private static bool IsTokenFresh(MarketplaceAccount account)
    {
        if (string.IsNullOrWhiteSpace(account.AccessToken))
        {
            return false;
        }

        if (account.ExpiresAt.Year < 2000)
        {
            return true;
        }

        return account.ExpiresAt > DateTime.UtcNow.AddMinutes(2);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static void ApplyBrowserHeaders(HttpRequestMessage request)
    {
        ApplyHeader(request.Headers, "User-Agent", PublicUserAgent);
        ApplyHeader(request.Headers, "Accept", "application/json");
        ApplyHeader(request.Headers, "Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8");
    }

    private static void ApplyHeader(HttpHeaders headers, string name, string value)
    {
        headers.Remove(name);
        if (!headers.TryAddWithoutValidation(name, value))
        {
            headers.Add(name, value);
        }
    }

    private sealed class MercadoLivreTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.Shopee.Payloads;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;
using TecFlow.Infrastructure.Services.LinkStrategies;

namespace TecFlow.Infrastructure.Services.Service.ExternalServices;

/// <summary>
/// Fallback oficial da Shopee Open API (get_item_base_info) quando o scrape/URL falha.
/// Usa AppKey/AppSecret persistidos em MarketplaceAccounts ou nas opções da integração.
/// </summary>
public sealed class ShopeeService : IShopeeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IShopeeIntegrationClient _shopeeClient;
    private readonly IMarketplaceSignatureService _signatureService;
    private readonly IMarketplaceAccountRepository _accountRepository;
    private readonly ShopeeIntegrationOptions _options;
    private readonly ILogger<ShopeeService> _logger;

    public ShopeeService(
        IShopeeIntegrationClient shopeeClient,
        IMarketplaceSignatureService signatureService,
        IMarketplaceAccountRepository accountRepository,
        IOptions<ShopeeIntegrationOptions> options,
        ILogger<ShopeeService> logger)
    {
        _shopeeClient = shopeeClient;
        _signatureService = signatureService;
        _accountRepository = accountRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ProductMetadataDto?> TryGetAffiliateItemDetailsAsync(
        string productUrl,
        IntegracaoLoja store,
        CancellationToken cancellationToken = default)
    {
        if (!ProductMetadataService.TryParseShopeeItemIds(productUrl, out var shopId, out var itemId)
            || !long.TryParse(itemId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var itemIdValue))
        {
            return null;
        }

        var accessToken = store.AccessToken?.Trim();
        MarketplaceAccount? account = null;
        if (!string.IsNullOrWhiteSpace(store.ShopId))
        {
            account = await _accountRepository.GetByShopAsync(store.ShopId, MarketplaceType.Shopee);
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            accessToken = account?.AccessToken?.Trim();
        }

        var resolvedShopId = FirstNonEmpty(store.ShopId, shopId, account?.ShopId);
        if (string.IsNullOrWhiteSpace(resolvedShopId) || string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogInformation(
                "Shopee Open API ignorada: loja sem ShopId/AccessToken. StoreId={StoreId}",
                store.Id);
            return null;
        }

        var (partnerId, partnerKey) = ResolveCredentials(account);
        if (string.IsNullOrWhiteSpace(partnerId) || string.IsNullOrWhiteSpace(partnerKey))
        {
            _logger.LogInformation(
                "Shopee Open API ignorada: AppKey/AppSecret ausentes. StoreId={StoreId}",
                store.Id);
            return null;
        }

        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var apiPath = NormalizePath(_options.GetItemBaseInfoPath);
            var sign = _signatureService.GenerateShopeeSign(
                partnerId,
                partnerKey,
                apiPath,
                timestamp,
                accessToken,
                resolvedShopId);

            var relative = $"{_options.GetItemBaseInfoPath.TrimStart('/')}?partner_id={Uri.EscapeDataString(partnerId)}"
                + $"&timestamp={timestamp}&sign={Uri.EscapeDataString(sign)}"
                + $"&access_token={Uri.EscapeDataString(accessToken)}"
                + $"&shop_id={Uri.EscapeDataString(resolvedShopId)}"
                + $"&item_id_list={itemIdValue}";

            using var response = await _shopeeClient.GetAsync(relative, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Shopee Open API recusou get_item_base_info. Status={Status} ItemId={ItemId}",
                    (int)response.StatusCode,
                    itemIdValue);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<ShopeeProductApiEnvelope<ShopeeGetItemBaseInfoResponsePayload>>(
                JsonOptions,
                cancellationToken);
            var item = payload?.Response?.ItemList?.FirstOrDefault();
            if (item is null)
            {
                return null;
            }

            var name = ProductMetadataService.NormalizePersistedProductName(item.ItemName);
            var price = item.PriceInfo?.FirstOrDefault()?.CurrentPrice;
            var image = item.Image?.ImageUrlList?.FirstOrDefault();
            if (name is null && price is not > 0 && string.IsNullOrWhiteSpace(image))
            {
                return null;
            }

            return new ProductMetadataDto
            {
                ProductName = name,
                ProductPrice = price is > 0 ? decimal.Round(price.Value, 2, MidpointRounding.AwayFromZero) : null,
                ProductImageUrl = image
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha no fallback Shopee Open API. Url={Url}", productUrl);
            return null;
        }
    }

    private (string PartnerId, string PartnerKey) ResolveCredentials(MarketplaceAccount? account)
    {
        var partnerId = _options.ResolveAffiliateAppId() ?? _options.PartnerId;
        var partnerKey = _options.ResolveAffiliateSecret() ?? _options.PartnerKey;

        if (account is not null)
        {
            if (!string.IsNullOrWhiteSpace(account.AppKey))
            {
                partnerId = account.AppKey.Trim();
            }

            if (!string.IsNullOrWhiteSpace(account.AppSecret))
            {
                partnerKey = account.AppSecret.Trim();
            }
        }

        return (partnerId ?? string.Empty, partnerKey ?? string.Empty);
    }

    private static string NormalizePath(string relativePath)
    {
        var trimmed = relativePath.Trim();
        if (!trimmed.StartsWith('/'))
        {
            trimmed = "/" + trimmed.TrimStart('/');
        }

        return trimmed.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : "/api/v2/" + trimmed.TrimStart('/');
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
}

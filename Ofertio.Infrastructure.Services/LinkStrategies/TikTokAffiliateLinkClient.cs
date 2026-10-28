using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Integrations.Common;
using Ofertio.Business.Integrations.TikTokShop;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.LinkStrategies;
using Ofertio.Core.Enums;
using Ofertio.Database;
using Ofertio.Database.Entity;

namespace Ofertio.Infrastructure.Services.LinkStrategies;

/// <summary>
/// Link de promoção oficial do TikTok Shop (Affiliate Creator, assinatura 202309) com o token de criador da loja do
/// cliente. Token vencido ou recusado é renovado via <c>refresh_token</c> e gravado na loja antes de repetir.
/// </summary>
public sealed class TikTokAffiliateLinkClient : ITikTokAffiliateLinkClient
{
    private static readonly TimeSpan ExpirySkew = TimeSpan.FromMinutes(5);

    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<TikTokShopIntegrationOptions> _options;
    private readonly ILogger<TikTokAffiliateLinkClient> _logger;
    private readonly IMarketplaceAuthService? _authService;
    private readonly IDbContextFactory<AppDbContext>? _dbContextFactory;

    public TikTokAffiliateLinkClient(
        HttpClient httpClient,
        IOptionsMonitor<TikTokShopIntegrationOptions> options,
        ILogger<TikTokAffiliateLinkClient> logger,
        IMarketplaceAuthService? authService = null,
        IDbContextFactory<AppDbContext>? dbContextFactory = null)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        _authService = authService;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<string> GenerateAffiliateLinkAsync(
        IntegracaoLoja store,
        string expandedProductUrl,
        string affiliateId,
        string? customNickname,
        CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        if (!options.HasAppCredentials)
        {
            throw new AffiliateLinkGenerationException(
                "Credenciais TikTok Shop não configuradas. Contacte o administrador do sistema.");
        }

        if (!TikTokShopCreatorLinkRules.HasCreatorToken(store.AccessToken))
        {
            throw new AffiliateLinkGenerationException(TikTokShopCreatorLinkRules.NoCreatorAccountWarning);
        }

        if (!TikTokShopProductUrlParser.TryParse(expandedProductUrl, out var productId))
        {
            throw new AffiliateLinkGenerationException(TikTokShopProductUrlParser.UnrecognizedLinkMessage);
        }

        var accessToken = store.ExpiresAt > DateTime.UtcNow.Add(ExpirySkew)
            ? store.AccessToken!.Trim()
            : await RefreshStoreTokenAsync(store, cancellationToken);

        var (link, refused, error) = await RequestPromotionLinkAsync(options, accessToken, productId, cancellationToken);
        if (refused)
        {
            accessToken = await RefreshStoreTokenAsync(store, cancellationToken);
            (link, refused, error) = await RequestPromotionLinkAsync(options, accessToken, productId, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(link))
        {
            throw new AffiliateLinkGenerationException(refused
                ? "O TikTok Shop recusou o token da sua conta de criador. Reconecte a conta em Minhas Lojas."
                : error ?? "O TikTok Shop não retornou um link de promoção. Verifique se o produto está em colaboração aberta.");
        }

        return link;
    }

    private async Task<(string? Link, bool Refused, string? Error)> RequestPromotionLinkAsync(
        TikTokShopIntegrationOptions options,
        string accessToken,
        string productId,
        CancellationToken cancellationToken)
    {
        const string body = "{}";
        var path = TikTokShopCreatorLinkRules.BuildPromotionLinkPath(options.AffiliatePromotionLinkPath, productId);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var appKey = options.AppKey.Trim();
        var sign = MarketplaceSignatureHelper.GenerateTikTokShopSign(appKey, options.AppSecret.Trim(), path, timestamp, body);
        var requestUri = new StringBuilder(options.ApiBaseUrl.TrimEnd('/'))
            .Append(path)
            .Append("?app_key=").Append(Uri.EscapeDataString(appKey))
            .Append("&timestamp=").Append(timestamp.ToString(CultureInfo.InvariantCulture))
            .Append("&sign=").Append(sign)
            .ToString();

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("x-tts-access-token", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var link = TikTokShopCreatorLinkRules.TryExtractPromotionLink(content, out var errorCode, out var errorMessage);
        if (TikTokShopAffiliateSearchRules.IsTokenRefused(response.StatusCode, errorCode))
        {
            return (null, true, null);
        }

        if (string.IsNullOrWhiteSpace(link))
        {
            _logger.LogWarning(
                "TikTok Shop promotion link falhou. Status={StatusCode} Code={Code} Message={Message} ProductId={ProductId}",
                (int)response.StatusCode,
                errorCode,
                errorMessage,
                productId);
            return (null, false, string.IsNullOrWhiteSpace(errorMessage)
                ? null
                : $"TikTok Shop recusou a geração do link: {errorMessage.Trim()}");
        }

        return (link, false, null);
    }

    /// <summary>Renova o token de criador da loja e grava em IntegracaoLojas e MarketplaceAccounts.</summary>
    private async Task<string> RefreshStoreTokenAsync(IntegracaoLoja store, CancellationToken cancellationToken)
    {
        if (_authService is null
            || string.IsNullOrWhiteSpace(store.RefreshToken)
            || HomologMarketplaceAuth.IsStubToken(store.RefreshToken))
        {
            throw new AffiliateLinkGenerationException(
                "O token da sua conta de criador TikTok expirou. Reconecte a conta em Minhas Lojas.");
        }

        var refreshed = await _authService.RefreshTikTokShopTokenAsync(store.RefreshToken.Trim(), cancellationToken);
        if (!refreshed.Success || string.IsNullOrWhiteSpace(refreshed.AccessToken))
        {
            throw new AffiliateLinkGenerationException(
                "Não foi possível renovar o token da sua conta de criador TikTok. Reconecte a conta em Minhas Lojas.");
        }

        store.AccessToken = refreshed.AccessToken.Trim();
        store.RefreshToken = string.IsNullOrWhiteSpace(refreshed.RefreshToken) ? store.RefreshToken : refreshed.RefreshToken.Trim();
        store.ExpiresAt = refreshed.ExpiresAt;
        await PersistStoreTokenAsync(store, refreshed.RefreshExpiresAt, cancellationToken);
        return store.AccessToken;
    }

    private async Task PersistStoreTokenAsync(IntegracaoLoja store, DateTime? refreshExpiresAt, CancellationToken cancellationToken)
    {
        if (_dbContextFactory is null)
        {
            return;
        }

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var integration = await dbContext.IntegracaoLojas
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(item => item.Id == store.Id, cancellationToken);
            if (integration is not null)
            {
                integration.AccessToken = store.AccessToken;
                integration.RefreshToken = store.RefreshToken;
                integration.ExpiresAt = store.ExpiresAt;
                integration.Status = MarketplaceIntegrationStatus.Active;
                integration.Touch();
            }

            var account = await dbContext.MarketplaceAccounts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    item => item.ShopId == store.ShopId && item.MarketplaceType == MarketplaceType.TikTokShop,
                    cancellationToken);
            if (account is not null)
            {
                account.AccessToken = store.AccessToken;
                account.RefreshToken = store.RefreshToken;
                account.ExpiresAt = store.ExpiresAt;
                account.RefreshExpiresAt = refreshExpiresAt ?? account.RefreshExpiresAt;
                account.Touch();
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Não foi possível gravar o token renovado da loja TikTok Shop {StoreId}.", store.Id);
        }
    }
}

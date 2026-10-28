using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;

namespace Ofertio.Tests.Unit.Integrations;

public class GlobalIntegrationRulesTests
{
    [Fact]
    public void BuildKey_ShouldNormalizeQueryAndIgnoreTrackingId()
    {
        var first = MercadoLivreSearchCacheRules.BuildKey("Dell  i7", new MercadoLivreCatalogSearchRequest { TrackingId = "111" });
        var second = MercadoLivreSearchCacheRules.BuildKey("  dell I7 ", new MercadoLivreCatalogSearchRequest { TrackingId = "222" });

        Assert.Equal(first, second);
    }

    [Fact]
    public void BuildKey_ShouldDifferWhenFiltersOrPageChange()
    {
        var baseKey = MercadoLivreSearchCacheRules.BuildKey("dell i7", new MercadoLivreCatalogSearchRequest());

        Assert.NotEqual(baseKey, MercadoLivreSearchCacheRules.BuildKey("dell i7", new MercadoLivreCatalogSearchRequest { Offset = 20 }));
        Assert.NotEqual(baseKey, MercadoLivreSearchCacheRules.BuildKey("dell i7", new MercadoLivreCatalogSearchRequest { MaxPrice = 3000m }));
        Assert.NotEqual(baseKey, MercadoLivreSearchCacheRules.BuildKey("dell i7", new MercadoLivreCatalogSearchRequest { FreeShippingOnly = true }));
        Assert.NotEqual(baseKey, MercadoLivreSearchCacheRules.BuildKey("dell i7", new MercadoLivreCatalogSearchRequest { DomainId = "MLB-NOTEBOOKS" }));
    }

    [Theory]
    [InlineData(null, 15)]
    [InlineData("", 15)]
    [InlineData("abc", 15)]
    [InlineData("30", 30)]
    [InlineData("0", 0)]
    [InlineData("-5", 0)]
    [InlineData("5000", 1440)]
    public void ResolveMinutes_ShouldFallBackToDefaultAndClamp(string? raw, int expected) =>
        Assert.Equal(expected, MercadoLivreSearchCacheRules.ResolveMinutes(raw));

    [Theory]
    [InlineData("10", true)]
    [InlineData("0", true)]
    [InlineData("1441", false)]
    [InlineData("-1", false)]
    [InlineData("x", false)]
    public void TryParseMinutes_ShouldAcceptOnlyTheConfiguredRange(string raw, bool expected) =>
        Assert.Equal(expected, MercadoLivreSearchCacheRules.TryParseMinutes(raw, out _));

    [Fact]
    public void ForAffiliate_ShouldApplyMattToolOnACopyWithoutTouchingTheCachedResult()
    {
        var cached = new OfficialCatalogChannelResult
        {
            Items = [new OfficialCatalogProductDto { ProductId = "MLB1", SourceUrl = "https://www.mercadolivre.com.br/dell/p/MLB1" }],
            NextOffset = 20,
            EffectiveQuery = "dell"
        };

        var copy = MercadoLivreSearchCacheRules.ForAffiliate(cached, "14343296");

        Assert.Contains("matt_tool=14343296", Assert.Single(copy.Items).SourceUrl, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("matt_tool", Assert.Single(cached.Items).SourceUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(20, copy.NextOffset);
        Assert.Equal("dell", copy.EffectiveQuery);
    }

    [Fact]
    public void IsCacheable_ShouldRejectFailuresAndMissingCredentials()
    {
        Assert.True(MercadoLivreSearchCacheRules.IsCacheable(OfficialCatalogChannelResult.Empty));
        Assert.False(MercadoLivreSearchCacheRules.IsCacheable(OfficialCatalogChannelResult.Unconfigured()));
        Assert.False(MercadoLivreSearchCacheRules.IsCacheable(OfficialCatalogChannelResult.Failed("403")));
    }

    [Fact]
    public void ResolveStatus_ShouldMapTokenLifecycle()
    {
        var now = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal(GlobalIntegrationRules.StatusDisconnected, GlobalIntegrationRules.ResolveStatus(null, now));
        Assert.Equal(GlobalIntegrationRules.StatusActive, GlobalIntegrationRules.ResolveStatus(Token(now.AddHours(5)), now));
        Assert.Equal(GlobalIntegrationRules.StatusActive, GlobalIntegrationRules.ResolveStatus(Token(now.AddHours(-1), "TG-1"), now));
        Assert.Equal(GlobalIntegrationRules.StatusExpired, GlobalIntegrationRules.ResolveStatus(Token(now.AddHours(-1)), now));
        Assert.Equal(GlobalIntegrationRules.StatusExpired, GlobalIntegrationRules.ResolveStatus(Token(now.AddHours(5), "TG-1", "recusado"), now));

        var refreshExpired = Token(now.AddHours(-1), "TG-1");
        refreshExpired.RefreshExpiresAt = now.AddMinutes(-1);
        Assert.Equal(GlobalIntegrationRules.StatusExpired, GlobalIntegrationRules.ResolveStatus(refreshExpired, now));
    }

    [Fact]
    public void Providers_ShouldExposeMercadoLivreTikTokShopAndPlannedApis()
    {
        var available = GlobalIntegrationRules.Providers.Where(provider => provider.Available).ToList();

        Assert.Equal(2, available.Count);
        Assert.Equal(GlobalIntegrationRules.MercadoLivrePath, available.Single(p => p.Key == "mercado-livre").Href);
        Assert.Equal(GlobalIntegrationRules.TikTokShopPath, available.Single(p => p.Key == "tiktok-shop").Href);
        Assert.Contains(GlobalIntegrationRules.Providers, provider => provider.Key == "shopee" && !provider.Available);
    }

    [Fact]
    public void ReturnPathFor_ShouldRouteByPlatformAndMasterFlag()
    {
        const string callback = "https://dev.ofertio.com.br/integracoes/oauth/callback";
        var masterTikTok = MarketplaceOAuthStateRules.Create(1, null, MarketplaceType.TikTokShop, null, callback) with { IsMaster = true };
        var clientTikTok = MarketplaceOAuthStateRules.Create(1, null, MarketplaceType.TikTokShop, null, callback);

        Assert.Equal("/master/integracoes-globais/tiktok-shop", MarketplaceOAuthStateRules.ReturnPathFor(masterTikTok));
        Assert.Equal("/marketplace-accounts", MarketplaceOAuthStateRules.ReturnPathFor(clientTikTok));
        Assert.Equal("TikTok Shop", clientTikTok.FriendlyName);
        Assert.Null(clientTikTok.CodeVerifier);
    }

    [Fact]
    public void BuildReturnUrl_ForMasterState_ShouldReturnToMasterPage()
    {
        var state = MarketplaceOAuthStateRules.Create(1, null, MarketplaceType.MercadoLivre, null, "https://dev.ofertio.com.br/integracoes/oauth/callback")
            with { IsMaster = true };

        var url = MarketplaceOAuthStateRules.BuildReturnUrl(state.CallbackUri, success: true, path: MarketplaceOAuthStateRules.ReturnPathFor(state));

        Assert.Equal("https://dev.ofertio.com.br/master/integracoes-globais/mercado-livre?status=success", url);
    }

    [Theory]
    [InlineData("/marketplace-accounts", true)]
    [InlineData("/master/integracoes-globais/mercado-livre", true)]
    [InlineData("/master/integracoes-globais/tiktok-shop", true)]
    [InlineData("/master/vault", false)]
    [InlineData("https://evil.example/marketplace-accounts", false)]
    [InlineData(null, false)]
    public void IsAllowedReturnPath_ShouldAcceptOnlyKnownPaths(string? path, bool expected) =>
        Assert.Equal(expected, MarketplaceOAuthStateRules.IsAllowedReturnPath(path));

    [Fact]
    public void BuildReturnUrl_WithUnknownPath_ShouldFallBackToAccounts()
    {
        var url = MarketplaceOAuthStateRules.BuildReturnUrl("https://localhost:7002/x", success: false, "falhou", "/master/vault");

        Assert.StartsWith("https://localhost:7002/marketplace-accounts?status=error", url, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_GlobalIntegrationsShouldBeAFeatureToggleControllingSearchScreens()
    {
        var feature = DefaultMenuCatalog.Find(DefaultMenuCatalog.GlobalIntegrations);

        Assert.NotNull(feature);
        Assert.True(feature!.IsFeatureToggle);
        Assert.False(feature.IsFolder);
        Assert.Null(feature.Route);
        var dependents = DefaultMenuCatalog.FeatureDependencies[DefaultMenuCatalog.GlobalIntegrations]
            .Select(key => DefaultMenuCatalog.Find(key)!.Route)
            .ToList();
        Assert.Equal(["/busca", "/radar-ofertas", "/radar/perfil"], dependents);
        Assert.DoesNotContain(DefaultMenuCatalog.BuildDefaultTree(), node => node.Key == DefaultMenuCatalog.GlobalIntegrations);
    }

    [Fact]
    public void ApplyFeatureDependencies_WhenFeatureHiddenForPlan_ShouldHideSearchRadarAndProfile()
    {
        var items = MenuSources();

        var hidden = MenuVisibilityRules.ApplyFeatureDependencies(items, new HashSet<int> { 1 });
        var tree = MenuVisibilityRules.BuildTree(items, hidden);

        Assert.Equal(new[] { 2, 3, 4 }, hidden.Where(id => id != 1).Order().ToArray());
        var routes = tree.SelectMany(node => node.Children).Select(node => node.Route).ToList();
        Assert.Equal(["/gerador-links"], routes);
    }

    [Fact]
    public void ApplyFeatureDependencies_WhenFeatureEnabled_ShouldKeepScreensAndHideOnlyTheToggleNode()
    {
        var items = MenuSources();

        var hidden = MenuVisibilityRules.ApplyFeatureDependencies(items, new HashSet<int>());
        var tree = MenuVisibilityRules.BuildTree(items, hidden);

        Assert.Empty(hidden);
        Assert.DoesNotContain(tree, node => node.Key == DefaultMenuCatalog.GlobalIntegrations);
        var routes = tree.SelectMany(node => node.Children).Select(node => node.Route).ToList();
        Assert.Equal(["/gerador-links", "/busca", "/radar-ofertas", "/radar/perfil"], routes);
    }

    [Fact]
    public void ApplyFeatureDependencies_WhenFeatureInactiveOrMissing_ShouldHideDependents()
    {
        var items = MenuSources().Where(item => item.Key != DefaultMenuCatalog.GlobalIntegrations).ToList();

        var hidden = MenuVisibilityRules.ApplyFeatureDependencies(items, new HashSet<int>());

        Assert.Equal(new[] { 2, 3, 4 }, hidden.Order().ToArray());
        Assert.Equal(DefaultMenuCatalog.GlobalIntegrations, MenuVisibilityRules.FeatureFor("integracoes.busca"));
        Assert.Null(MenuVisibilityRules.FeatureFor("integracoes.gerador"));
    }

    private static GlobalIntegrationToken Token(DateTime expiresAt, string? refreshToken = null, string? lastError = null) => new()
    {
        Provider = MarketplaceType.MercadoLivre,
        AccessToken = "APP_USR-master",
        RefreshToken = refreshToken,
        ExpiresAt = expiresAt,
        LastError = lastError
    };

    private static List<MenuNodeSource> MenuSources() =>
    [
        new(1, DefaultMenuCatalog.GlobalIntegrations, "Integrações Globais", null, "bi-globe-americas", null, 25, true),
        new(10, "integracoes.links", "Links & Ofertas", null, "bi-link-45deg", null, 50, true, IsFolder: true),
        new(5, "integracoes.gerador", "Gerador de Links", "/gerador-links", "bi-link-45deg", 10, 10, true),
        new(2, DefaultMenuCatalog.OfferSearch, "Buscar Ofertas", "/busca", "bi-search", 10, 20, true),
        new(3, DefaultMenuCatalog.RadarOffers, "Radar de Ofertas", "/radar-ofertas", "bi-broadcast", 10, 30, true),
        new(4, DefaultMenuCatalog.RadarProfile, "Perfil de mineração", "/radar/perfil", "bi-sliders", 10, 40, true)
    ];
}

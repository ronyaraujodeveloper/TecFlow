using System.Security.Claims;
using TecFlow.Business.Dto;
using TecFlow.Business.Service.Master;
using TecFlow.SharedUi.Services.Auth;
using TecFlow.SharedUi.Services.Navigation;

namespace TecFlow.Tests.Unit.Master;

public class MenuTreeEditRulesTests
{
    [Theory]
    [InlineData(4, 1, false, new[] { 4, 1, 2, 3 })]
    [InlineData(4, 1, true, new[] { 1, 4, 2, 3 })]
    [InlineData(1, 3, true, new[] { 2, 3, 1 })]
    [InlineData(3, 1, false, new[] { 3, 1, 2 })]
    public void InsertRelativeTo_ShouldPlaceItemAroundTarget(int itemId, int targetId, bool after, int[] expected)
    {
        var siblings = new[] { 1, 2, 3 };

        Assert.Equal(expected, MenuTreeEditRules.InsertRelativeTo(siblings, itemId, targetId, after));
    }

    [Fact]
    public void IsSelfOrDescendant_ShouldDetectCycles()
    {
        var parents = new Dictionary<int, int?> { [1] = null, [2] = 1, [3] = 2, [4] = null };

        Assert.True(MenuTreeEditRules.IsSelfOrDescendant(parents, 3, 1));
        Assert.True(MenuTreeEditRules.IsSelfOrDescendant(parents, 1, 1));
        Assert.False(MenuTreeEditRules.IsSelfOrDescendant(parents, 4, 1));
        Assert.False(MenuTreeEditRules.IsSelfOrDescendant(parents, 1, 3));
    }

    [Theory]
    [InlineData(null, "Informe")]
    [InlineData("   ", "Informe")]
    public void ValidateFolderTitle_ShouldRequireTitle(string? title, string expected)
    {
        Assert.Contains(expected, MenuTreeEditRules.ValidateFolderTitle(title));
        Assert.Null(MenuTreeEditRules.ValidateFolderTitle("Ferramentas"));
        Assert.NotNull(MenuTreeEditRules.ValidateFolderTitle(new string('x', MenuTreeEditRules.FolderTitleMaxLength + 1)));
    }
}

public class MasterRoutesTests
{
    [Theory]
    [InlineData("/master", true)]
    [InlineData("/master/", true)]
    [InlineData("/Master/Tenants", true)]
    [InlineData("master/menu-permissions?x=1", true)]
    [InlineData("/mastermind", false)]
    [InlineData("/marketplace-accounts", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsMasterRoute_ShouldMatchOnlyMasterPrefix(string? path, bool expected)
    {
        Assert.Equal(expected, MasterRoutes.IsMasterRoute(path));
    }

    [Fact]
    public void ImpersonationExit_ShouldBeTheOnlyGuardExemption()
    {
        Assert.True(MasterRoutes.IsGuardExempt("/master/emular/sair"));
        Assert.True(MasterRoutes.IsGuardExempt("/master/emular/sair?x=1"));
        Assert.False(MasterRoutes.IsGuardExempt("/master/tenants"));
        Assert.False(MasterRoutes.IsGuardExempt("/master/emular/0b5c1c2e-0000-0000-0000-000000000000"));
    }

    [Fact]
    public void Links_ShouldAllLiveUnderMasterPrefixAndBeUnique()
    {
        Assert.All(MasterRoutes.Links, link => Assert.True(MasterRoutes.IsMasterRoute(link.Href)));
        Assert.Equal(MasterRoutes.Links.Count, MasterRoutes.Links.Select(link => link.Href).Distinct().Count());
        Assert.Contains(MasterRoutes.Links, link => link.Href == MasterRoutes.DeveloperAccounts);
        Assert.Contains(MasterRoutes.Links, link => link.Href == MasterRoutes.MenuPermissions);
    }

    [Fact]
    public void SuporteTecnico_ShouldNotSeeKeyOrPlanLinks()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, MasterRoles.SuporteTecnico)], "test"));

        var visible = MasterRoutes.Links
            .Where(link => link.Permission is null || MasterAccess.Has(user, link.Permission))
            .Select(link => link.Href)
            .ToList();

        Assert.DoesNotContain(MasterRoutes.DeveloperAccounts, visible);
        Assert.DoesNotContain(MasterRoutes.MenuPermissions, visible);
        Assert.Contains(MasterRoutes.Tenants, visible);
    }
}

public class MenuCatalogTests
{
    [Fact]
    public void Catalog_ShouldHaveUniqueKeysAndValidParents()
    {
        var keys = DefaultMenuCatalog.All.Select(item => item.Key).ToList();

        Assert.Equal(keys.Count, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(DefaultMenuCatalog.All.Where(item => item.ParentKey is not null), item =>
        {
            var parentIndex = keys.IndexOf(item.ParentKey!);
            Assert.True(parentIndex >= 0 && parentIndex < keys.IndexOf(item.Key), $"Pai de {item.Key} deve vir antes no catálogo.");
        });
    }

    [Fact]
    public void Catalog_ShouldNotExposeRoadmapItemsToClients()
    {
        var roadmap = DefaultMenuCatalog.Find(DefaultMenuCatalog.Roadmap)!;
        var fallbackRoutes = AllRoutes(DefaultMenuCatalog.BuildDefaultTree()).ToList();

        Assert.True(roadmap.IsFolder);
        Assert.True(roadmap.HiddenForAllPlans);
        Assert.Equal(6, DefaultMenuCatalog.All.Count(item => item.ParentKey == DefaultMenuCatalog.Roadmap));
        Assert.Equal("EM BREVE", DefaultMenuCatalog.All.Single(item => item.Route == "/fila-engajamento").Badge);
        Assert.DoesNotContain("/campanhas", fallbackRoutes);
        Assert.DoesNotContain("/saude-sistema", fallbackRoutes);
        Assert.Contains("/dashboard", fallbackRoutes);
        Assert.Contains("/minha-conta", fallbackRoutes);
    }

    private static IEnumerable<string> AllRoutes(IEnumerable<ClientMenuNodeDto> nodes) =>
        nodes.SelectMany(node => new[] { node.Route }.Concat(AllRoutes(node.Children))).OfType<string>();

    [Fact]
    public void Radar_ShouldBeHiddenByDefaultOnlyForStarter()
    {
        var radar = DefaultMenuCatalog.Find(DefaultMenuCatalog.RadarOffers)!;

        Assert.Equal([SaasPlanCodes.Starter], radar.HiddenForPlans);
    }

    [Fact]
    public void BuildDefaultTree_ShouldNestChildrenInOrder()
    {
        var tree = DefaultMenuCatalog.BuildDefaultTree();

        Assert.Equal("painel", tree[0].Key);
        var integracoes = tree.Single(node => node.Key == "integracoes");
        Assert.Equal(DefaultMenuCatalog.MarketplaceAccounts, integracoes.Children[0].Key);
        Assert.Equal("/marketplace-accounts", integracoes.Children[0].Route);
        Assert.Equal(4, integracoes.Children.Count);
        var links = integracoes.Children.Single(node => node.Key == "integracoes.links");
        Assert.Equal(["/gerador-links", "/busca", "/radar-ofertas", "/radar/perfil"], links.Children.Select(node => node.Route));
    }

    [Fact]
    public void Catalog_ShouldGroupChannelsAndKeepCategoriesWithoutRoute()
    {
        var tree = DefaultMenuCatalog.BuildDefaultTree();

        var categories = tree.Where(node => node.Route is null).Select(node => node.Key).ToList();
        Assert.Equal(["mensageria", "canais", "integracoes"], categories);
        var canais = tree.Single(node => node.Key == "canais");
        Assert.Equal(["whatsapp", "telegram"], canais.Children.Select(node => node.Key));
        Assert.All(canais.Children, parent => Assert.Equal(2, parent.Children.Count));
    }
}

public class MenuVisibilityRulesTests
{
    private static readonly MenuNodeSource[] Items =
    [
        new(1, "painel", "Painel", "/dashboard", "bi", null, 10, true),
        new(2, "grupo", "Grupo", null, "bi", null, 20, true),
        new(3, "grupo.a", "A", "/a", "bi", 2, 20, true),
        new(4, "grupo.b", "B", "/b", "bi", 2, 10, true),
        new(5, "vazio", "Vazio", null, "bi", null, 30, true),
        new(6, "inativo", "Inativo", "/x", "bi", null, 40, false)
    ];

    [Fact]
    public void BuildTree_ShouldDropEmptyGroupsAndInactiveItems()
    {
        var tree = MenuVisibilityRules.BuildTree(Items, new HashSet<int>());

        Assert.Equal(["painel", "grupo"], tree.Select(node => node.Key));
        Assert.Equal(["grupo.b", "grupo.a"], tree[1].Children.Select(node => node.Key));
    }

    [Fact]
    public void BuildTree_HiddenGroup_ShouldHideItsChildren()
    {
        var tree = MenuVisibilityRules.BuildTree(Items, new HashSet<int> { 2 });

        Assert.Equal(["painel"], tree.Select(node => node.Key));
    }

    [Fact]
    public void BuildTree_GroupWithAllChildrenHidden_ShouldDisappear()
    {
        var tree = MenuVisibilityRules.BuildTree(Items, new HashSet<int> { 3, 4 });

        Assert.DoesNotContain(tree, node => node.Key == "grupo");
    }

    [Fact]
    public void Flatten_ShouldListParentBeforeChildrenWithDepth()
    {
        var flat = MenuVisibilityRules.Flatten(Items);

        var keys = flat.Select(entry => entry.Item.Key).ToList();
        Assert.True(keys.IndexOf("grupo") < keys.IndexOf("grupo.b"));
        Assert.Equal(1, flat.Single(entry => entry.Item.Key == "grupo.a").Depth);
        Assert.Equal(Items.Length, flat.Count);
    }
}

public class DeveloperAccountRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ExpirationStatus_ShouldFlagExpiringWindow()
    {
        Assert.Equal(SecretExpirationStatus.None, DeveloperAccountRules.ExpirationStatus(null, Now));
        Assert.Equal(SecretExpirationStatus.Expired, DeveloperAccountRules.ExpirationStatus(Now.AddMinutes(-1), Now));
        Assert.Equal(SecretExpirationStatus.Expiring, DeveloperAccountRules.ExpirationStatus(Now.AddDays(10), Now));
        Assert.Equal(SecretExpirationStatus.Valid, DeveloperAccountRules.ExpirationStatus(Now.AddDays(60), Now));
    }

    [Theory]
    [InlineData("Tiktok", "App", "x", "id", true, "Plataforma")]
    [InlineData("Shopee", "Outro", "x", "id", true, "Tipo")]
    [InlineData("Shopee", "App", " ", "id", true, "nome")]
    [InlineData("Shopee", "App", "x", "", true, "exigem")]
    [InlineData("Shopee", "App", "x", "id", false, "exigem")]
    public void Validate_ShouldExplainProblem(string platform, string kind, string label, string clientId, bool hasSecret, string expected)
    {
        Assert.Contains(expected, DeveloperAccountRules.Validate(platform, kind, label, clientId, hasSecret));
    }

    [Fact]
    public void Validate_TestAccount_ShouldNotRequireCredentials()
    {
        Assert.Null(DeveloperAccountRules.Validate(DeveloperPlatforms.Amazon, DeveloperAccountKinds.Test, "QA", null, false));
    }
}

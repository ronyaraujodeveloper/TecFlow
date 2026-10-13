using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Master;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;
using TecFlow.Infrastructure.Services.Master;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.Master;

public class MenuPermissionServiceTests
{
    private static readonly MasterActor Actor = new(1, "admin@tecflow.com", MasterRoles.SuperAdmin, null);

    private static AppDbContext CreateContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(service => service.Encrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        encryption.Setup(service => service.Decrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options, encryption.Object, new NullCurrentTenantService());
    }

    private static async Task<(AppDbContext Context, MenuPermissionService Service, SaasPlan Starter, SaasPlan Pro)> SeedAsync()
    {
        var context = CreateContext();
        var starter = new SaasPlan { Code = SaasPlanCodes.Starter, Name = "Starter", IsActive = true, SortOrder = 1 };
        var pro = new SaasPlan { Code = SaasPlanCodes.Pro, Name = "Pro", IsActive = true, SortOrder = 2 };
        context.SaasPlans.AddRange(starter, pro);
        var starterTenant = new Tenant { Id = Guid.NewGuid(), Name = "Loja Starter", PlanCode = SaasPlanCodes.Starter };
        var proTenant = new Tenant { Id = Guid.NewGuid(), Name = "Loja Pro", PlanCode = SaasPlanCodes.Pro };
        context.Tenants.AddRange(starterTenant, proTenant);
        context.UserAccounts.AddRange(
            new UserAccount { Id = 10, Name = "Starter", Email = "s@x.com", PasswordHash = "h", TenantId = starterTenant.Id },
            new UserAccount { Id = 11, Name = "Pro", Email = "p@x.com", PasswordHash = "h", TenantId = proTenant.Id },
            new UserAccount { Id = 12, Name = "Admin", Email = "a@x.com", PasswordHash = "h", TenantId = starterTenant.Id, Role = MasterRoles.SuperAdmin });
        await context.SaveChangesAsync();
        var service = new MenuPermissionService(context, new MemoryCache(new MemoryCacheOptions()), new Mock<IMasterAuditService>().Object);
        return (context, service, starter, pro);
    }

    private static IEnumerable<string> AllRoutes(IEnumerable<ClientMenuNodeDto> nodes) =>
        nodes.SelectMany(node => new[] { node.Route }.Concat(AllRoutes(node.Children))).OfType<string>();

    [Fact]
    public async Task EnsureCatalog_ShouldSeedTreeOnceWithStarterRadarHidden()
    {
        var (context, service, starter, _) = await SeedAsync();
        await using var scope = context;

        await service.EnsureCatalogAsync();
        await service.EnsureCatalogAsync();

        Assert.Equal(DefaultMenuCatalog.All.Count, await context.MenuItems.CountAsync());
        var hidden = await context.PlanMenuPermissions.Where(item => !item.IsVisible).ToListAsync();
        Assert.Equal(2, hidden.Count);
        Assert.All(hidden, item => Assert.Equal(starter.Id, item.PlanId));
        var radar = await context.MenuItems.SingleAsync(item => item.Key == DefaultMenuCatalog.RadarOffers);
        var parent = await context.MenuItems.SingleAsync(item => item.Key == "integracoes.links");
        Assert.Equal(parent.Id, radar.ParentId);
    }

    [Fact]
    public async Task EnsureCatalog_ShouldMoveLegacyFlatItemsIntoNewHierarchy()
    {
        var (context, service, starter, _) = await SeedAsync();
        await using var scope = context;
        var integracoes = new MenuItem { Key = "integracoes", Title = "Integrações", Icon = "bi-plug", Order = 50 };
        var whatsapp = new MenuItem { Key = "whatsapp", Title = "WhatsApp", Icon = "bi-whatsapp", Order = 30 };
        var radar = new MenuItem { Key = DefaultMenuCatalog.RadarOffers, Title = "Radar", Route = "/radar-ofertas", Icon = "bi-broadcast", Order = 40, Parent = integracoes };
        context.MenuItems.AddRange(integracoes, whatsapp, radar);
        context.PlanMenuPermissions.Add(new PlanMenuPermission { PlanId = starter.Id, MenuItem = radar, IsVisible = false });
        await context.SaveChangesAsync();

        await service.EnsureCatalogAsync();

        var items = await context.MenuItems.AsNoTracking().ToListAsync();
        var byKey = items.ToDictionary(item => item.Key);
        Assert.Equal(DefaultMenuCatalog.All.Count, items.Count);
        Assert.Equal(byKey["canais"].Id, byKey["whatsapp"].ParentId);
        Assert.Equal(byKey["integracoes.links"].Id, byKey[DefaultMenuCatalog.RadarOffers].ParentId);
        Assert.Equal("Radar de Ofertas", byKey[DefaultMenuCatalog.RadarOffers].Title);
        Assert.Equal(1, await context.PlanMenuPermissions.CountAsync(item => item.MenuItemId == radar.Id));
    }

    [Fact]
    public async Task ClientMenu_ShouldFollowTenantPlanAndExemptMasters()
    {
        var (context, service, _, _) = await SeedAsync();
        await using var scope = context;

        var starterMenu = AllRoutes(await service.GetClientMenuAsync(10)).ToList();
        var proMenu = AllRoutes(await service.GetClientMenuAsync(11)).ToList();
        var masterMenu = AllRoutes(await service.GetClientMenuAsync(12)).ToList();

        Assert.DoesNotContain("/radar-ofertas", starterMenu);
        Assert.Contains("/radar-ofertas", proMenu);
        Assert.Contains("/radar-ofertas", masterMenu);
        Assert.Contains("/dashboard", starterMenu);
    }

    [Fact]
    public async Task SaveMatrix_ShouldHideItemForPlanAndDeactivateGlobally()
    {
        var (context, service, _, pro) = await SeedAsync();
        await using var scope = context;
        var matrix = await service.GetMatrixAsync();
        var busca = matrix.Rows.Single(row => row.Route == "/busca");
        var estudio = matrix.Rows.Single(row => row.Route == "/estudio-midia");
        Assert.True(busca.Visibility[pro.Id]);

        var result = await service.SaveMatrixAsync(Actor, new MenuPermissionSaveDto
        {
            Cells = [new MenuPermissionCellDto { PlanId = pro.Id, MenuItemId = busca.MenuItemId, IsVisible = false }],
            ActiveItems = new Dictionary<int, bool> { [estudio.MenuItemId] = false }
        });

        Assert.True(result.Success);
        var proMenu = AllRoutes(await service.GetClientMenuAsync(11)).ToList();
        var masterMenu = AllRoutes(await service.GetClientMenuAsync(12)).ToList();
        Assert.DoesNotContain("/busca", proMenu);
        Assert.DoesNotContain("/estudio-midia", proMenu);
        Assert.Contains("/busca", masterMenu);
        Assert.DoesNotContain("/estudio-midia", masterMenu);
        Assert.False((await service.GetMatrixAsync()).Rows.Single(row => row.Route == "/busca").Visibility[pro.Id]);
    }

    [Fact]
    public async Task SaveMatrix_WithoutChanges_ShouldReportNothingToSave()
    {
        var (context, service, _, _) = await SeedAsync();
        await using var scope = context;

        var result = await service.SaveMatrixAsync(Actor, new MenuPermissionSaveDto());

        Assert.Equal("Nenhuma alteração no menu.", result.Message);
    }
}

public class DeveloperAccountServiceTests
{
    private static readonly MasterActor Actor = new(1, "admin@tecflow.com", MasterRoles.SuperAdmin, null);

    private static AppDbContext CreateContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(service => service.Encrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        encryption.Setup(service => service.Decrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options, encryption.Object, new NullCurrentTenantService());
    }

    private static DeveloperAccountService CreateService(
        AppDbContext context,
        Mock<IDeveloperAccountHealthChecker>? checker = null) =>
        new(
            context,
            (checker ?? new Mock<IDeveloperAccountHealthChecker>()).Object,
            new Mock<IMasterAuditService>().Object);

    private static DeveloperAccountDto MlApp() => new()
    {
        Platform = DeveloperPlatforms.MercadoLivre,
        Kind = DeveloperAccountKinds.App,
        Label = "App oficial",
        ClientId = "1234567890",
        ClientSecret = "segredo-super-secreto",
        Extra = "https://api.tecflow/cb",
        IsActive = true
    };

    [Fact]
    public async Task Save_ShouldMaskSecretAndKeepItWhenMaskedValueComesBack()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var (created, error) = await service.SaveAsync(Actor, MlApp());

        Assert.Null(error);
        Assert.Equal("••••reto", created!.ClientSecret);
        created.Label = "Renomeado";
        var (updated, _) = await service.SaveAsync(Actor, created);
        Assert.Equal("Renomeado", updated!.Label);
        Assert.Equal("segredo-super-secreto", (await context.SystemDeveloperAccounts.SingleAsync()).ClientSecret);
        Assert.Equal(0, await context.MarketplaceAccounts.CountAsync());
    }

    [Fact]
    public async Task Save_InvalidAppWithoutSecret_ShouldFail()
    {
        await using var context = CreateContext();
        var request = MlApp();
        request.ClientSecret = null;

        var (item, error) = await CreateService(context).SaveAsync(Actor, request);

        Assert.Null(item);
        Assert.Contains("segredo", error);
    }

    [Fact]
    public async Task Save_TestAccount_ShouldEncryptPasswordWithoutTouchingTheVault()
    {
        await using var context = CreateContext();
        var (account, error) = await CreateService(context).SaveAsync(Actor, new DeveloperAccountDto
        {
            Platform = DeveloperPlatforms.Shopee,
            Kind = DeveloperAccountKinds.Test,
            Environment = DeveloperEnvironments.Sandbox,
            Label = "Comprador QA",
            TestUsername = "qa@tecflow.com",
            TestPassword = "senha-de-teste"
        });

        Assert.Null(error);
        Assert.True(account!.HasTestPassword);
        Assert.Equal("••••este", account.TestPassword);
        Assert.Equal(0, await context.SystemSettings.CountAsync());
    }

    [Fact]
    public async Task Check_ShouldPersistLastDiagnosticAndWarnExpiringSecret()
    {
        await using var context = CreateContext();
        var checker = new Mock<IDeveloperAccountHealthChecker>();
        checker.Setup(item => item.CheckAsync(DeveloperPlatforms.MercadoLivre, "1234567890", "segredo-super-secreto", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeveloperCredentialCheck(true, "Token emitido.", 120));
        var service = CreateService(context, checker: checker);
        var request = MlApp();
        request.SecretExpiresAt = DateTime.UtcNow.AddDays(5);
        var (account, _) = await service.SaveAsync(Actor, request);

        var result = await service.CheckAsync(account!.Id);

        Assert.True(result.Success);
        Assert.Contains("vence em", result.Message);
        var stored = await context.SystemDeveloperAccounts.SingleAsync();
        Assert.True(stored.LastCheckOk);
        Assert.NotNull(stored.LastCheckAt);
    }

}

public class GlobalCredentialHealthServiceTests
{
    [Fact]
    public async Task Check_ShouldUseConfiguredVaultKeysWithShopeeFallbacks()
    {
        var checker = new Mock<IDeveloperAccountHealthChecker>();
        checker.Setup(item => item.CheckAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string platform, string? id, string? secret, string? extra, CancellationToken _) =>
                new DeveloperCredentialCheck(!string.IsNullOrEmpty(id), $"{platform}:{id}:{extra}", 1));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Integrations:Shopee:PartnerId"] = "partner",
            ["Integrations:Shopee:AppKey"] = "appkey",
            ["Integrations:Shopee:AppSecret"] = "s",
            ["Integrations:AmazonPaApi:AccessKey"] = "AK",
            ["Integrations:AmazonPaApi:SecretKey"] = "SK",
            ["Integrations:AmazonPaApi:PartnerTag"] = "loja-20"
        }).Build();

        var results = await new GlobalCredentialHealthService(checker.Object, configuration).CheckAsync();

        Assert.False(results.Single(item => item.Platform == DeveloperPlatforms.MercadoLivre).Success);
        Assert.Equal("Shopee:appkey:", results.Single(item => item.Platform == DeveloperPlatforms.Shopee).Message);
        Assert.Equal("Amazon:AK:loja-20", results.Single(item => item.Platform == DeveloperPlatforms.Amazon).Message);
    }
}

public class DeveloperAccountHealthCheckerTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (DeveloperAccountHealthChecker Checker, StubHandler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        return (new DeveloperAccountHealthChecker(new HttpClient(handler), NullLogger<DeveloperAccountHealthChecker>.Instance), handler);
    }

    [Fact]
    public async Task MercadoLivre_ValidCredentials_ShouldReportTokenLifetime()
    {
        var (checker, handler) = Create(_ => Json(HttpStatusCode.OK, "{\"access_token\":\"APP_USR\",\"expires_in\":21600}"));

        var result = await checker.CheckAsync(DeveloperPlatforms.MercadoLivre, "123", "sec", null);

        Assert.True(result.Success);
        Assert.Contains("6h", result.Message);
        Assert.Equal(DeveloperAccountHealthChecker.MercadoLivreTokenUrl, handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task MercadoLivre_InvalidCredentials_ShouldExposeApiMessage()
    {
        var (checker, _) = Create(_ => Json(HttpStatusCode.BadRequest, "{\"message\":\"invalid client_id or client_secret.\",\"error\":\"invalid_client\"}"));

        var result = await checker.CheckAsync(DeveloperPlatforms.MercadoLivre, "123", "errado", null);

        Assert.False(result.Success);
        Assert.Contains("invalid client_id", result.Message);
    }

    [Fact]
    public async Task Shopee_GraphQlError_ShouldFailWithCode()
    {
        var (checker, handler) = Create(_ => Json(HttpStatusCode.OK,
            "{\"errors\":[{\"message\":\"error\",\"extensions\":{\"code\":10020,\"message\":\"Invalid Signature\"}}]}"));

        var result = await checker.CheckAsync(DeveloperPlatforms.Shopee, "app", "sec", null);

        Assert.False(result.Success);
        Assert.Contains("10020 Invalid Signature", result.Message);
        Assert.StartsWith("SHA256 Credential=app, Timestamp=", handler.Requests[0].Headers.GetValues("Authorization").Single());
    }

    [Fact]
    public async Task Amazon_WithoutPartnerTag_ShouldNotCallServer()
    {
        var (checker, handler) = Create(_ => Json(HttpStatusCode.OK, "{}"));

        var result = await checker.CheckAsync(DeveloperPlatforms.Amazon, "AK", "SK", null);

        Assert.False(result.Success);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Amazon_SignedRequest_ShouldTargetPaApiWithSigV4()
    {
        var (checker, handler) = Create(_ => Json(HttpStatusCode.Unauthorized,
            "{\"Errors\":[{\"Code\":\"InvalidSignature\",\"Message\":\"The request has not been correctly signed.\"}]}"));

        var result = await checker.CheckAsync(DeveloperPlatforms.Amazon, "AK", "SK", "loja-20");

        Assert.False(result.Success);
        Assert.Contains("InvalidSignature", result.Message);
        var request = handler.Requests.Single();
        Assert.Equal("https://webservices.amazon.com.br/paapi5/searchitems", request.RequestUri!.ToString());
        Assert.StartsWith("AWS4-HMAC-SHA256 Credential=AK/", request.Headers.GetValues("Authorization").Single());
    }

    [Fact]
    public async Task MissingCredentials_ShouldShortCircuit()
    {
        var (checker, handler) = Create(_ => Json(HttpStatusCode.OK, "{}"));

        var result = await checker.CheckAsync(DeveloperPlatforms.MercadoLivre, "", null, null);

        Assert.False(result.Success);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void ShopeeAuthorization_ShouldFollowOfficialFormula()
    {
        var header = DeveloperAccountHealthChecker.BuildShopeeAuthorization("123", "secret", 1700000000, "{}");
        var expected = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("1231700000000{}secret"))).ToLowerInvariant();

        Assert.Equal($"SHA256 Credential=123, Timestamp=1700000000, Signature={expected}", header);
    }
}

using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ofertio.API.Security;
using Ofertio.Core.Entities;

namespace Ofertio.Tests.Unit.Security;

/// <summary>
/// Auditoria A1: a API não pode ter endpoint anônimo por esquecimento.
/// </summary>
public class ApiAuthorizationPolicyTests
{
    private static readonly Assembly ApiAssembly = typeof(OfertioAuthorizationExtensions).Assembly;

    /// <summary>
    /// Endpoints anônimos aprovados. Incluir algo aqui é decisão de segurança:
    /// o endpoint precisa ter outra proteção (assinatura de webhook, token de uso único etc.).
    /// </summary>
    private static readonly string[] ApprovedAnonymousActions =
    [
        "AsaasWebhookController.*",
        "AuthController.TikTokLogin",
        "AuthController.ShopeeLogin",
        "AuthController.RequestPasswordResetAsync",
        "AuthController.ConfirmPasswordResetAsync",
        "AuthController.RegisterAsync",
        "JoinRedirectController.*",
        "LegalDocumentsController.GetActiveAsync",
        "MarketplaceAuthController.GetAuthorizationUrl",
        "MarketplaceAuthController.GetPlatformAuthorizationUrl",
        "MarketplaceAuthController.CompleteMercadoLivreCallbackAsync",
        "MarketplaceAuthController.CompleteSealedCallbackAsync",
        "MarketplaceAuthController.VincularManualAsync", // pendente: bloco F da auditoria
        "MercadoLivreController.*",
        "PublicConverterController.GetBySlugAsync",
        "PublicConverterController.ConvertAsync",
        "PublicPlansController.*",
        "ShopeeWebhookController.*",
        "ShortLinkRedirectController.*",
        "SocialMediaWebhookController.*", // pendente: bloco F da auditoria
        "TelegramWebhookController.*",
        "TikTokShopWebhookController.*",
        "WhatsAppWebhookController.*",
    ];

    [Fact]
    public void FallbackPolicy_ExigeUsuarioAutenticado()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOfertioAuthorization();

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.FallbackPolicy);
        Assert.Contains(
            options.FallbackPolicy!.Requirements,
            requirement => requirement is Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public void FallbackPolicy_MantemPoliciesDoMaster()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOfertioAuthorization();

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.GetPolicy(
            Ofertio.Business.Service.Master.MasterPermissions.PolicyName(
                Ofertio.Business.Service.Master.MasterPermissions.All.First())));
    }

    [Fact]
    public void EndpointsAnonimos_SaoSomenteOsAprovados()
    {
        var anonymous = ApiAssembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .SelectMany(type => ActionMethods(type).Select(method => (type, method)))
            .Where(pair => IsAnonymous(pair.type, pair.method))
            .Select(pair => $"{pair.type.Name}.{pair.method.Name}")
            .Distinct()
            .OrderBy(name => name)
            .ToList();

        var unapproved = anonymous.Where(name => !IsApproved(name)).ToList();

        Assert.True(
            unapproved.Count == 0,
            "Endpoints anônimos não aprovados: " + string.Join(", ", unapproved));
    }

    [Fact]
    public void RotaApiUsuarios_NaoExisteMaisNaApi()
    {
        var routes = ApiAssembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
            .Select(route => route.Template);

        Assert.DoesNotContain(routes, template =>
            string.Equals(template, "api/Usuarios", StringComparison.OrdinalIgnoreCase));
    }

    // GET /api/Usuarios não entra aqui: casa com a rota anônima do link curto /{slug}/{code},
    // que só procura um link com esse código. A remoção da rota é coberta por RotaApiUsuarios_NaoExisteMaisNaApi.
    [Theory]
    [InlineData("POST", "/api/Usuarios")]
    [InlineData("DELETE", "/api/Usuarios/1")]
    [InlineData("GET", "/api/Produtos")]
    [InlineData("GET", "/api/Afiliados/1")]
    [InlineData("GET", "/api/master/me")]
    [InlineData("GET", "/WeatherForecast")]
    [InlineData("GET", "/rota-que-nao-existe")]
    public async Task Pipeline_RequisicaoAnonima_RecebeUnauthorized(string method, string path)
    {
        await using var app = await StartApiPipelineAsync();
        var client = app.GetTestClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void UserAccount_SerializadoEmJson_NaoExpoeHashNemTokens()
    {
        var user = new UserAccount
        {
            Name = "Fulano",
            Email = "fulano@example.com",
            PasswordHash = "$2a$11$hash-que-nao-pode-vazar",
            TikTokShopAccessToken = "access-que-nao-pode-vazar",
            TikTokRefreshToken = "refresh-que-nao-pode-vazar"
        };

        var json = JsonSerializer.Serialize(user, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        });

        Assert.DoesNotContain("nao-pode-vazar", json);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fulano@example.com", json);
    }

    private static async Task<WebApplication> StartApiPipelineAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services
            .AddControllers()
            .AddApplicationPart(ApiAssembly);
        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddOfertioAuthorization();

        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        return app;
    }

    private static IEnumerable<MethodInfo> ActionMethods(Type controller) =>
        controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName
                && method.GetCustomAttribute<NonActionAttribute>() is null
                && method.GetCustomAttributes().Any(attribute =>
                    attribute is Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute or RouteAttribute));

    private static bool IsAnonymous(Type controller, MethodInfo method) =>
        method.GetCustomAttributes(inherit: true).OfType<IAllowAnonymous>().Any()
        || controller.GetCustomAttributes(inherit: true).OfType<IAllowAnonymous>().Any();

    private static bool IsApproved(string action) =>
        ApprovedAnonymousActions.Any(approved =>
            approved.EndsWith(".*", StringComparison.Ordinal)
                ? action.StartsWith(approved[..^1], StringComparison.Ordinal)
                : string.Equals(approved, action, StringComparison.Ordinal));

    /// <summary>Autentica só quem manda o header X-Test-User; o resto é anônimo.</summary>
    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-User", out var user))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.ToString())], SchemeName);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }
}

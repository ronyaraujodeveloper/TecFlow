using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Security;
using Ofertio.Infrastructure.Services.Security;

namespace Ofertio.Tests.Unit.Security;

/// <summary>
/// Auditoria A2: login social Apple/Facebook não pode aceitar token forjado ou de outro app,
/// e o vínculo automático por e-mail só acontece com e-mail verificado pelo provedor.
/// </summary>
public class SocialLoginProviderValidationTests
{
    private const string AppleClientId = "br.com.ofertio.web";
    private const string FacebookAppId = "1234567890";
    private const string FacebookAppSecret = "segredo-de-teste";

    // ---------- Apple ----------

    [Fact]
    public async Task Apple_TokenAssinadoPelaApple_EhAceito()
    {
        var apple = new FakeAppleKeys();
        var validator = CreateValidator(apple.Handler(), appleClientId: AppleClientId);

        var result = await validator.ValidateAsync("Apple", null, apple.CreateToken());

        Assert.NotNull(result);
        Assert.Equal("ana@privaterelay.appleid.com", result!.Email);
        Assert.Equal("apple-sub-1", result.ProviderKey);
        Assert.True(result.EmailVerified);
    }

    [Fact]
    public async Task Apple_TokenSemAssinatura_EhRecusado()
    {
        var apple = new FakeAppleKeys();
        var validator = CreateValidator(apple.Handler(), appleClientId: AppleClientId);

        var result = await validator.ValidateAsync("Apple", null, UnsignedToken(apple.KeyId));

        Assert.Null(result);
    }

    [Fact]
    public async Task Apple_TokenAssinadoComChaveDoAtacante_EhRecusado()
    {
        var apple = new FakeAppleKeys();
        var attacker = new FakeAppleKeys(keyId: apple.KeyId);
        var validator = CreateValidator(apple.Handler(), appleClientId: AppleClientId);

        var result = await validator.ValidateAsync("Apple", null, attacker.CreateToken());

        Assert.Null(result);
    }

    [Fact]
    public async Task Apple_TokenParaOutroApp_EhRecusado()
    {
        var apple = new FakeAppleKeys();
        var validator = CreateValidator(apple.Handler(), appleClientId: AppleClientId);

        var result = await validator.ValidateAsync("Apple", null, apple.CreateToken(audience: "com.outro.app"));

        Assert.Null(result);
    }

    [Fact]
    public async Task Apple_TokenVencido_EhRecusado()
    {
        var apple = new FakeAppleKeys();
        var validator = CreateValidator(apple.Handler(), appleClientId: AppleClientId);

        var result = await validator.ValidateAsync("Apple", null, apple.CreateToken(expires: DateTime.UtcNow.AddMinutes(-10)));

        Assert.Null(result);
    }

    [Fact]
    public async Task Apple_TokenDeOutroEmissor_EhRecusado()
    {
        var apple = new FakeAppleKeys();
        var validator = CreateValidator(apple.Handler(), appleClientId: AppleClientId);

        var result = await validator.ValidateAsync("Apple", null, apple.CreateToken(issuer: "https://evil.example.com"));

        Assert.Null(result);
    }

    [Fact]
    public async Task Apple_SemClientIdConfigurado_RecusaSemConsultarAApple()
    {
        var apple = new FakeAppleKeys();
        var handler = apple.Handler();
        var validator = CreateValidator(handler, appleClientId: null);

        var result = await validator.ValidateAsync("Apple", null, apple.CreateToken());

        Assert.Null(result);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Apple_EmailNaoVerificado_VoltaComEmailVerifiedFalso()
    {
        var apple = new FakeAppleKeys();
        var validator = CreateValidator(apple.Handler(), appleClientId: AppleClientId);

        var result = await validator.ValidateAsync("Apple", null, apple.CreateToken(emailVerified: "false"));

        Assert.NotNull(result);
        Assert.False(result!.EmailVerified);
    }

    // ---------- Facebook ----------

    [Fact]
    public async Task Facebook_TokenDoNossoApp_EhAceitoSemEmailVerificado()
    {
        var handler = FacebookHandler(appId: FacebookAppId, isValid: true, debugUserId: "fb-1", profileId: "fb-1");
        var validator = CreateValidator(handler, facebookConfigured: true);

        var result = await validator.ValidateAsync("Facebook", "user-access-token", null);

        Assert.NotNull(result);
        Assert.Equal("fb-1", result!.ProviderKey);
        Assert.False(result.EmailVerified);
    }

    [Fact]
    public async Task Facebook_SegredoDoAppVaiNoCabecalho_NuncaNaUrl()
    {
        var handler = FacebookHandler(appId: FacebookAppId, isValid: true, debugUserId: "fb-1", profileId: "fb-1");
        var validator = CreateValidator(handler, facebookConfigured: true);

        await validator.ValidateAsync("Facebook", "user-access-token", null);

        var debug = Assert.Single(handler.Requests, request => request.Uri.Contains("debug_token", StringComparison.Ordinal));
        Assert.DoesNotContain(FacebookAppSecret, debug.Uri, StringComparison.Ordinal);
        Assert.Equal($"Bearer {FacebookAppId}|{FacebookAppSecret}", debug.Authorization);
        Assert.All(handler.Requests, request => Assert.DoesNotContain(FacebookAppSecret, request.Uri, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Facebook_TokenEmitidoParaOutroApp_EhRecusado()
    {
        var handler = FacebookHandler(appId: "999-outro-app", isValid: true, debugUserId: "fb-1", profileId: "fb-1");
        var validator = CreateValidator(handler, facebookConfigured: true);

        var result = await validator.ValidateAsync("Facebook", "user-access-token", null);

        Assert.Null(result);
    }

    [Fact]
    public async Task Facebook_TokenInvalido_EhRecusado()
    {
        var handler = FacebookHandler(appId: FacebookAppId, isValid: false, debugUserId: "fb-1", profileId: "fb-1");
        var validator = CreateValidator(handler, facebookConfigured: true);

        var result = await validator.ValidateAsync("Facebook", "user-access-token", null);

        Assert.Null(result);
    }

    [Fact]
    public async Task Facebook_PerfilDeOutroUsuario_EhRecusado()
    {
        var handler = FacebookHandler(appId: FacebookAppId, isValid: true, debugUserId: "fb-1", profileId: "fb-2");
        var validator = CreateValidator(handler, facebookConfigured: true);

        var result = await validator.ValidateAsync("Facebook", "user-access-token", null);

        Assert.Null(result);
    }

    [Fact]
    public async Task Facebook_SemAppConfigurado_RecusaSemChamarOFacebook()
    {
        var handler = FacebookHandler(appId: FacebookAppId, isValid: true, debugUserId: "fb-1", profileId: "fb-1");
        var validator = CreateValidator(handler, facebookConfigured: false);

        var result = await validator.ValidateAsync("Facebook", "user-access-token", null);

        Assert.Null(result);
        Assert.Empty(handler.Requests);
    }

    // ---------- Google (email_verified) ----------

    [Theory]
    [InlineData("""{"sub":"g-1","aud":"google-client","email":"ana@gmail.com","email_verified":"true"}""", true)]
    [InlineData("""{"sub":"g-1","aud":"google-client","email":"ana@gmail.com","email_verified":true}""", true)]
    [InlineData("""{"sub":"g-1","aud":"google-client","email":"ana@gmail.com","email_verified":"false"}""", false)]
    [InlineData("""{"sub":"g-1","aud":"google-client","email":"ana@gmail.com"}""", false)]
    public async Task Google_EmailVerified_AceitaTextoOuBooleano(string json, bool expected)
    {
        var handler = new RoutingHandler().Route("oauth2.googleapis.com/tokeninfo", json);
        var validator = CreateValidator(handler, googleClientId: "google-client");

        var result = await validator.ValidateAsync("Google", null, "id-token");

        Assert.NotNull(result);
        Assert.Equal(expected, result!.EmailVerified);
    }

    // ---------- Regra de vínculo ----------

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void VinculoAutomatico_SoComEmailVerificado(bool verified, bool expected)
    {
        var payload = new SocialAuthTokenPayload { Email = "ana@ofertio.com.br", ProviderKey = "x", EmailVerified = verified };

        Assert.Equal(expected, SocialAccountLinkRules.CanAutoLinkByEmail(payload));
    }

    // ---------- Infraestrutura de teste ----------

    private static SocialAuthTokenValidator CreateValidator(
        RoutingHandler handler,
        string? appleClientId = null,
        bool facebookConfigured = false,
        string? googleClientId = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Authentication:Apple:ClientId"] = appleClientId,
            ["Authentication:Google:ClientId"] = googleClientId
        };
        if (facebookConfigured)
        {
            settings["Authentication:Facebook:ClientId"] = FacebookAppId;
            settings["Authentication:Facebook:ClientSecret"] = FacebookAppSecret;
        }

        return new SocialAuthTokenValidator(
            new SingleHandlerFactory(handler),
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
            NullLogger<SocialAuthTokenValidator>.Instance);
    }

    private static RoutingHandler FacebookHandler(string appId, bool isValid, string debugUserId, string profileId) =>
        new RoutingHandler()
            .Route("graph.facebook.com/debug_token", JsonSerializer.Serialize(new
            {
                data = new { app_id = appId, is_valid = isValid, user_id = debugUserId }
            }))
            .Route("graph.facebook.com/me", JsonSerializer.Serialize(new
            {
                id = profileId,
                name = "Ana",
                email = "ana@ofertio.com.br"
            }));

    private static string UnsignedToken(string keyId)
    {
        static string Encode(string json) => Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json));
        var header = Encode($$"""{"alg":"none","kid":"{{keyId}}","typ":"JWT"}""");
        var exp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
        var body = Encode($$"""{"iss":"{{SocialAuthTokenValidator.AppleIssuer}}","aud":"{{AppleClientId}}","sub":"apple-sub-1","email":"vitima@ofertio.com.br","email_verified":"true","exp":{{exp}}}""");
        return $"{header}.{body}.";
    }

    /// <summary>Par de chaves RSA no papel da Apple: publica o JWKS e assina id_tokens.</summary>
    private sealed class FakeAppleKeys
    {
        private readonly RSA _rsa = RSA.Create(2048);

        public FakeAppleKeys(string? keyId = null) => KeyId = keyId ?? Guid.NewGuid().ToString("N");

        public string KeyId { get; }

        public RoutingHandler Handler()
        {
            var publicKey = new RsaSecurityKey(_rsa.ExportParameters(includePrivateParameters: false)) { KeyId = KeyId };
            var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(publicKey);
            var jwks = JsonSerializer.Serialize(new
            {
                keys = new[] { new { kty = jwk.Kty, kid = jwk.Kid, use = "sig", alg = "RS256", n = jwk.N, e = jwk.E } }
            });
            return new RoutingHandler().Route("appleid.apple.com/auth/keys", jwks);
        }

        public string CreateToken(
            string audience = AppleClientId,
            string issuer = SocialAuthTokenValidator.AppleIssuer,
            DateTime? expires = null,
            string emailVerified = "true")
        {
            var expiresAt = expires ?? DateTime.UtcNow.AddMinutes(10);
            return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Audience = audience,
                IssuedAt = expiresAt.AddMinutes(-20),
                NotBefore = expiresAt.AddMinutes(-20),
                Expires = expiresAt,
                Claims = new Dictionary<string, object>
                {
                    ["sub"] = "apple-sub-1",
                    ["email"] = "ana@privaterelay.appleid.com",
                    ["email_verified"] = emailVerified
                },
                SigningCredentials = new SigningCredentials(
                    new RsaSecurityKey(_rsa) { KeyId = KeyId },
                    SecurityAlgorithms.RsaSha256)
            });
        }
    }

    private sealed record CapturedRequest(string Uri, string? Authorization);

    /// <summary>Responde por trecho da URL e guarda cada requisição feita.</summary>
    private sealed class RoutingHandler : HttpMessageHandler
    {
        private readonly List<(string Fragment, string Json)> _routes = [];

        public List<CapturedRequest> Requests { get; } = [];

        public RoutingHandler Route(string fragment, string json)
        {
            _routes.Add((fragment, json));
            return this;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!.ToString();
            Requests.Add(new CapturedRequest(uri, request.Headers.Authorization?.ToString()));
            var route = _routes.FirstOrDefault(item => uri.Contains(item.Fragment, StringComparison.Ordinal));
            var response = route.Json is null
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(route.Json, Encoding.UTF8, "application/json")
                };
            return Task.FromResult(response);
        }
    }

    private sealed class SingleHandlerFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public SingleHandlerFactory(HttpMessageHandler handler) => _handler = handler;

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }
}

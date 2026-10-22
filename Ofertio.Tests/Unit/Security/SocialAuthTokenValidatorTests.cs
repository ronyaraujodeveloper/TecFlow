using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Ofertio.Infrastructure.Services.Security;

namespace Ofertio.Tests.Unit.Security;

public class SocialAuthTokenValidatorTests
{
    private const string ClientId = "ofertio-test-client";

    [Fact]
    public async Task ValidateAsync_ShouldAcceptGoogleTokenIssuedForTheApplication()
    {
        var validator = Create(ClientId, """{"sub":"sub-1","aud":"ofertio-test-client","email":"ana@ofertio.com","name":"Ana"}""");

        var result = await validator.ValidateAsync("Google", accessToken: null, idToken: "id-token");

        Assert.NotNull(result);
        Assert.Equal("ana@ofertio.com", result!.Email);
        Assert.Equal("sub-1", result.ProviderKey);
        Assert.Equal("Ana", result.DisplayName);
    }

    [Fact]
    public async Task ValidateAsync_ShouldRejectGoogleTokenIssuedForAnotherClient()
    {
        var validator = Create(ClientId, """{"sub":"sub-1","aud":"outro-client","email":"ana@ofertio.com"}""");

        var result = await validator.ValidateAsync("Google", accessToken: null, idToken: "id-token");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_ShouldIgnoreGoogleAccessTokenWhenIdTokenIsMissing()
    {
        var validator = Create(ClientId, """{"sub":"sub-1","aud":"ofertio-test-client","email":"ana@ofertio.com"}""");

        var result = await validator.ValidateAsync("Google", accessToken: "ya29.token", idToken: null);

        Assert.Null(result);
    }

    private static SocialAuthTokenValidator Create(string clientId, string json)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = clientId
            })
            .Build();

        return new SocialAuthTokenValidator(
            new StubHttpClientFactory(json),
            configuration,
            NullLogger<SocialAuthTokenValidator>.Instance);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly string _json;

        public StubHttpClientFactory(string json) => _json = json;

        public HttpClient CreateClient(string name) =>
            new(new StubHandler(_json), disposeHandler: true);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _json;

        public StubHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}

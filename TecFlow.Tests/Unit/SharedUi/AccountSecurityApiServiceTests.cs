using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Dto.Auth;
using TecFlow.SharedUi.Services.Auth;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.UI;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.SharedUi;

public class AccountSecurityApiServiceTests
{
    [Fact]
    public async Task GetProviderStatusAsync_ShouldDeserializeValidEnvelope()
    {
        var handler = StubHttpMessageHandler.WithJsonResponse(
            """{"status":true,"descricao":"OK","data":{"provider":"local","hasPassword":true,"linkedProviders":["Google"]}}""");

        var api = CreateApi(handler);
        var result = await api.GetProviderStatusAsync();

        Assert.True(result.Status);
        Assert.True(result.Data!.HasPassword);
        Assert.Contains("Google", result.Data.LinkedProviders);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnFriendlyError_WhenPayloadIsNotJson()
    {
        var handler = StubHttpMessageHandler.WithJsonResponse("<html>502</html>", HttpStatusCode.BadGateway);
        var api = CreateApi(handler);

        var result = await api.ChangePasswordAsync(new ChangePasswordDto
        {
            CurrentPassword = "old",
            NewPassword = "new-password"
        });

        Assert.False(result.Status);
        Assert.Equal("Não foi possível interpretar a resposta do servidor.", result.Descricao);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnFriendlyError_WhenStatusIsNumeric()
    {
        var handler = StubHttpMessageHandler.WithJsonResponse(
            """{"title":"Bad Request","status":400,"detail":"senha inválida"}""",
            HttpStatusCode.BadRequest);

        var api = CreateApi(handler);
        var result = await api.ChangePasswordAsync(new ChangePasswordDto
        {
            CurrentPassword = "x",
            NewPassword = "y"
        });

        Assert.False(result.Status);
        Assert.Equal("Não foi possível interpretar a resposta do servidor.", result.Descricao);
    }

    private static AccountSecurityApiService CreateApi(HttpMessageHandler handler)
    {
        var loading = new Mock<ILoadingService>();
        loading.Setup(service => service.BeginScope(It.IsAny<string?>())).Returns(new NoopDisposable());
        return new AccountSecurityApiService(
            new NamedClientFactory(handler),
            new StaticTokenProvider("jwt"),
            loading.Object,
            NullLogger<AccountSecurityApiService>.Instance);
    }

    private sealed class NamedClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public NamedClientFactory(HttpMessageHandler handler)
        {
            _client = new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://localhost:7001/")
            };
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class StaticTokenProvider : IAccessTokenProvider
    {
        private readonly string? _token;

        public StaticTokenProvider(string? token) => _token = token;

        public string? GetAccessToken() => _token;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

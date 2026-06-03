using System.Net;
using System.Net.Http.Json;
using TecFlow.Util.Address;
using TecFlow.Util.Validation;

namespace TecFlow.Tests.Unit;

public class ValidationHelperCepTests
{
    [Theory]
    [InlineData("01310100")]
    [InlineData("01310-100")]
    [InlineData(" 01310-100 ")]
    public void IsValidCep_ReturnsTrue_ForValidFormat(string cep)
    {
        Assert.True(ValidationHelper.IsValidCep(cep));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("01310-10")]
    [InlineData("013101000")]
    [InlineData("0131A-100")]
    public void IsValidCep_ReturnsFalse_ForInvalidFormat(string? cep)
    {
        Assert.False(ValidationHelper.IsValidCep(cep));
    }

    [Fact]
    public void NormalizeCepDigits_ReturnsEightDigits_WhenCepIsValid()
    {
        Assert.Equal("01310100", ValidationHelper.NormalizeCepDigits("01310-100"));
    }
}

public class CepServiceTests
{
    [Fact]
    public async Task SearchCepAsync_ReturnsNull_WhenCepIsInvalid()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var result = await service.SearchCepAsync("invalid");

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchCepAsync_ReturnsNull_WhenViaCepReportsNotFound()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { erro = true })
        });

        var result = await service.SearchCepAsync("01310-100");

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchCepAsync_ReturnsMappedAddress_WhenViaCepReturnsData()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                cep = "01310-100",
                logradouro = "Avenida Paulista",
                bairro = "Bela Vista",
                localidade = "São Paulo",
                uf = "SP"
            })
        });

        var result = await service.SearchCepAsync("01310-100");

        Assert.NotNull(result);
        Assert.Equal("01310-100", result.ZipCode);
        Assert.Equal("Avenida Paulista", result.Street);
        Assert.Equal("Bela Vista", result.Neighborhood);
        Assert.Equal("São Paulo", result.City);
        Assert.Equal("SP", result.State);
    }

    [Fact]
    public async Task SearchCepAsync_ReturnsNull_WhenConnectionFails()
    {
        var handler = new ThrowingHttpMessageHandler();
        var service = new CepService(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://viacep.com.br/ws/")
        });

        var result = await service.SearchCepAsync("01310-100");

        Assert.Null(result);
    }

    private static CepService CreateService(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new DelegatingHandlerStub(responder);
        return new CepService(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://viacep.com.br/ws/")
        });
    }

    private sealed class DelegatingHandlerStub(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Connection failed.");
    }
}

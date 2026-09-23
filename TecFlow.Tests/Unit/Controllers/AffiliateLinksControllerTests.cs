using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.API.Controllers;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Database.Filter;

namespace TecFlow.Tests.Unit.Controllers;

public class AffiliateLinksControllerTests
{
    [Fact]
    public void GerarLinkAfiliadoDto_ShouldDeserializeFormJson()
    {
        const string json =
            """{"originalUrl":"https://shopee.com.br/produto-i.1.2","storeId":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee","shopId":"123456","customNickname":"cadeira","tenantId":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"}""";

        var dto = JsonSerializer.Deserialize<GerarLinkAfiliadoDto>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(dto);
        Assert.Equal("https://shopee.com.br/produto-i.1.2", dto!.OriginalUrl);
        Assert.Equal("123456", dto.ShopId);
        Assert.Equal(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), dto.StoreId);
        Assert.Empty(dto.StoreIds);
    }

    [Fact]
    public void GerarLinkAfiliadoDto_ShouldResolveStoreScopesFromStoreIds()
    {
        var first = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var second = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
        var dto = new GerarLinkAfiliadoDto
        {
            StoreId = first,
            StoreIds = [first, second]
        };

        var scopes = dto.ResolveStoreScopes();
        Assert.Equal(2, scopes.Count);
        Assert.Contains(first, scopes);
        Assert.Contains(second, scopes);
    }

    [Fact]
    public async Task GenerateAsync_ShouldReturn401_WhenUserIsMissing()
    {
        var controller = CreateController(userId: null);
        var action = await controller.GenerateAsync(new GerarLinkAfiliadoDto(), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(action.Result);
        Assert.Equal(401, unauthorized.StatusCode);
        Assert.False(Assert.IsType<GerarLinkAfiliadoResponseDto>(unauthorized.Value).Success);
    }

    [Fact]
    public async Task GenerateAsync_ShouldReturn500Envelope_WhenServiceThrows()
    {
        var generation = new Mock<IAffiliateLinkGenerationService>();
        generation.Setup(s => s.GenerateAsync(It.IsAny<GerarLinkAfiliadoDto>(), 7, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha"));

        var controller = CreateController(generation.Object);
        var action = await controller.GenerateAsync(new GerarLinkAfiliadoDto { OriginalUrl = "https://shopee.com.br/x" }, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(500, status.StatusCode);
        var body = Assert.IsType<GerarLinkAfiliadoResponseDto>(status.Value);
        Assert.False(body.Success);
        Assert.Contains("inesperado", body.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_ShouldReturnOk_WhenGenerationSucceeds()
    {
        var generation = new Mock<IAffiliateLinkGenerationService>();
        generation.Setup(s => s.GenerateAsync(It.IsAny<GerarLinkAfiliadoDto>(), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GerarLinkAfiliadoResponseDto
            {
                Success = true,
                Message = "OK",
                OriginalUrl = "https://shopee.com.br/produto",
                AffiliateUrl = "https://shopee.com.br/produto?tracking_code=tecflow_sandbox_subid",
                ConvertedUrl = "https://shopee.com.br/produto?tracking_code=tecflow_sandbox_subid",
                ShortenedUrl = "http://localhost:5001/r/abc1234",
                PlatformDetected = "Shopee"
            });

        var controller = CreateController(generation.Object);
        var action = await controller.GenerateAsync(
            new GerarLinkAfiliadoDto { OriginalUrl = "https://shopee.com.br/produto" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var body = Assert.IsType<GerarLinkAfiliadoResponseDto>(ok.Value);
        Assert.Equal("https://shopee.com.br/produto", body.OriginalUrl);
        Assert.Equal("https://shopee.com.br/produto?tracking_code=tecflow_sandbox_subid", body.AffiliateUrl);
        Assert.Equal("http://localhost:5001/r/abc1234", body.ShortenedUrl);
    }

    [Fact]
    public async Task GenerateAsync_ShouldReturn400_WhenShopeeParseFails()
    {
        var generation = new Mock<IAffiliateLinkGenerationService>();
        generation.Setup(s => s.GenerateAsync(It.IsAny<GerarLinkAfiliadoDto>(), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GerarLinkAfiliadoResponseDto
            {
                Success = false,
                Status = false,
                Message = ShopeeProductUrlParser.UnrecognizedLinkMessage,
                Descricao = ShopeeProductUrlParser.UnrecognizedLinkMessage
            });

        var controller = CreateController(generation.Object);
        var action = await controller.GenerateAsync(
            new GerarLinkAfiliadoDto { OriginalUrl = "https://shopee.com.br/categoria/moveis" },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
        Assert.Equal(400, badRequest.StatusCode);
        var body = Assert.IsType<GerarLinkAfiliadoResponseDto>(badRequest.Value);
        Assert.False(body.Success);
        Assert.Equal(ShopeeProductUrlParser.UnrecognizedLinkMessage, body.Message);
    }

    [Fact]
    public async Task ListHistoryAsync_ShouldReturn401_WhenUserIsMissing()
    {
        var controller = CreateController(userId: null);
        var action = await controller.ListHistoryAsync(new AffiliateLinkFilter(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(action.Result);
    }

    private static AffiliateLinksController CreateController(
        IAffiliateLinkGenerationService? generation = null,
        string? userId = "7")
    {
        var controller = new AffiliateLinksController(
            generation ?? new Mock<IAffiliateLinkGenerationService>().Object,
            new Mock<IAffiliateLinkHistoryService>().Object,
            new Mock<IAffiliateLinkGenerationContext>().Object,
            NullLogger<AffiliateLinksController>.Instance);

        var claims = userId is null
            ? Array.Empty<Claim>()
            : [new Claim(ClaimTypes.NameIdentifier, userId)];
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: userId is null ? null : "Test"))
            }
        };
        return controller;
    }
}

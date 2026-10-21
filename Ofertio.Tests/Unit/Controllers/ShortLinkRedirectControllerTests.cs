using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ofertio.API.Controllers;
using Ofertio.Business.Interfaces.Repositories;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Util.Text;

namespace Ofertio.Tests.Unit.Controllers;

public class ShortLinkRedirectControllerTests
{
    [Fact]
    public async Task RedirectAsync_ShouldReturnNotFound_WhenCodeIsInvalid()
    {
        var controller = CreateController(new Mock<IShortAffiliateLinkRepository>().Object, new Mock<ILinkClickTelemetryService>().Object);

        var result = await controller.RedirectAsync("loja", "ab", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RedirectAsync_ShouldEnqueueTelemetryAndRedirect()
    {
        var linkId = Guid.NewGuid();
        var links = new Mock<IShortAffiliateLinkRepository>();
        links.Setup(s => s.GetByShortCodeAsync("abc1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShortAffiliateLink
            {
                AffiliateLinkId = linkId,
                ShortCode = "abc1234",
                DestinationUrl = "https://shopee.com.br/produto",
                OriginalUrl = "https://shopee.com.br/produto",
                PlatformType = MarketplaceType.Shopee
            });

        var telemetry = new Mock<ILinkClickTelemetryService>();
        var controller = CreateController(links.Object, telemetry.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.RedirectAsync("AchadinhosDeAaz", "ABC1234", CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://shopee.com.br/produto", redirect.Url);
        telemetry.Verify(
            s => s.EnqueueClickLog(
                linkId,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task RedirectLegacyAsync_ShouldResolveByCodeWithoutStoreSlug()
    {
        var linkId = Guid.NewGuid();
        var links = new Mock<IShortAffiliateLinkRepository>();
        links.Setup(s => s.GetByShortCodeAsync("abc1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShortAffiliateLink
            {
                AffiliateLinkId = linkId,
                ShortCode = "abc1234",
                DestinationUrl = "https://shopee.com.br/produto",
                OriginalUrl = "https://shopee.com.br/produto",
                PlatformType = MarketplaceType.Shopee
            });

        var telemetry = new Mock<ILinkClickTelemetryService>();
        var controller = CreateController(links.Object, telemetry.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.RedirectLegacyAsync("abc1234", CancellationToken.None);

        Assert.IsType<RedirectResult>(result);
        telemetry.Verify(
            s => s.EnqueueClickLog(
                linkId,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Once);
    }

    [Theory]
    [InlineData("97303724", true)]
    [InlineData("AchadinhoCerto", true)]
    [InlineData("lojaApi", true)]
    [InlineData("Achadinho-Certo", false)]
    [InlineData("_blazor", false)]
    public void StoreRedirectTemplate_ShouldAcceptSlugsGeneratedBySlugHelper(string storeSlug, bool expected)
    {
        var template = ShortLinkRedirectController.StoreRedirectTemplate.Replace("[[", "[").Replace("]]", "]");
        var pattern = RoutePatternFactory.Parse(template);
        var policy = pattern.GetParameter("storeSlug")!.ParameterPolicies.Single().Content!;
        var regex = policy["regex(".Length..^1];

        Assert.Equal(expected, Regex.IsMatch(storeSlug, regex));
        Assert.Matches(regex, SlugHelper.GenerateSlug(storeSlug));
    }

    [Fact]
    public async Task RedirectAsync_ShouldRedirect_WhenStoreSlugIsNumeric()
    {
        var links = new Mock<IShortAffiliateLinkRepository>();
        links.Setup(s => s.GetByShortCodeAsync("xh8pmzh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShortAffiliateLink
            {
                AffiliateLinkId = Guid.NewGuid(),
                ShortCode = "xh8pmzh",
                DestinationUrl = "https://www.mercadolivre.com.br/p/MLB20621890?matt_tool=97303724&matt_word=97303724",
                OriginalUrl = "https://www.mercadolivre.com.br/p/MLB20621890",
                PlatformType = MarketplaceType.MercadoLivre
            });
        var controller = CreateController(links.Object, new Mock<ILinkClickTelemetryService>().Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.RedirectAsync("97303724", "xh8pmzh", CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("/p/MLB20621890", redirect.Url);
    }

    private static ShortLinkRedirectController CreateController(
        IShortAffiliateLinkRepository links,
        ILinkClickTelemetryService telemetry) =>
        new(links, telemetry, NullLogger<ShortLinkRedirectController>.Instance);
}
